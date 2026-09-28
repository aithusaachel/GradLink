using System.Text.Json;
using GradLink.API.Data;
using GradLink.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GradLink.API.Tests;

public sealed class NotificationApiTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Applying_notifies_the_employer()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        var job = await employer.PostJobAsync();

        await graduate.ApplySuccessfullyAsync(job.Id);

        var notification = Assert.Single(await employer.GetNotificationsAsync());
        Assert.Contains(graduate.FullName, notification.Message);
        Assert.Contains(job.Title, notification.Message);
        Assert.False(notification.IsRead);
        Assert.Equal(1, await employer.GetUnreadCountAsync());
    }

    [Fact]
    public async Task Status_changes_notify_the_graduate()
    {
        var scenario = await factory.CreateApplicationAsync();

        (await scenario.Employer.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Shortlisted)).EnsureSuccessStatusCode();

        var notification = Assert.Single(await scenario.Graduate.GetNotificationsAsync());
        Assert.Contains(scenario.Job.Title, notification.Message);
        Assert.Contains(nameof(ApplicationStatus.Shortlisted), notification.Message);
    }

    [Fact]
    public async Task Setting_an_unchanged_status_does_not_notify_again()
    {
        var scenario = await factory.CreateApplicationAsync();

        (await scenario.Employer.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Shortlisted)).EnsureSuccessStatusCode();
        (await scenario.Employer.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Shortlisted)).EnsureSuccessStatusCode();

        Assert.Single(await scenario.Graduate.GetNotificationsAsync());
    }

    [Fact]
    public async Task Users_cannot_mark_another_users_notification_as_read()
    {
        var scenario = await factory.CreateApplicationAsync();
        var outsider = await factory.RegisterAsync(UserRole.Employer);
        var notification = Assert.Single(await scenario.Employer.GetNotificationsAsync());

        var response = await outsider.Client.PutAsync($"api/notifications/{notification.Id}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await scenario.Employer.GetUnreadCountAsync());
    }

    [Fact]
    public async Task Marking_a_notification_read_updates_the_unread_count()
    {
        var scenario = await factory.CreateApplicationAsync();
        var notification = Assert.Single(await scenario.Employer.GetNotificationsAsync());

        (await scenario.Employer.Client.PutAsync($"api/notifications/{notification.Id}/read", null)).EnsureSuccessStatusCode();

        Assert.Equal(0, await scenario.Employer.GetUnreadCountAsync());
        Assert.True(Assert.Single(await scenario.Employer.GetNotificationsAsync()).IsRead);
    }

    [Fact]
    public async Task Mark_all_as_read_clears_the_unread_count()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var job = await employer.PostJobAsync();
        for (var i = 0; i < 3; i++)
            await (await factory.RegisterAsync(UserRole.Graduate)).ApplySuccessfullyAsync(job.Id);
        Assert.Equal(3, await employer.GetUnreadCountAsync());

        (await employer.Client.PutAsync("api/notifications/read-all", null)).EnsureSuccessStatusCode();

        Assert.Equal(0, await employer.GetUnreadCountAsync());
    }

    [Fact]
    public async Task History_is_newest_first_and_pages_with_skip_and_take()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var job = await employer.PostJobAsync();
        var applicants = new List<TestUser>();
        for (var i = 0; i < 3; i++)
        {
            var graduate = await factory.RegisterAsync(UserRole.Graduate);
            await graduate.ApplySuccessfullyAsync(job.Id);
            applicants.Add(graduate);
        }

        var firstPage = await employer.GetNotificationsAsync("?take=2");
        var secondPage = await employer.GetNotificationsAsync("?skip=2&take=2");

        Assert.Equal(2, firstPage.Count);
        Assert.Contains(applicants[2].FullName, firstPage[0].Message);
        Assert.Contains(applicants[1].FullName, firstPage[1].Message);
        Assert.Contains(applicants[0].FullName, Assert.Single(secondPage).Message);
    }

    [Fact]
    public async Task History_pages_never_exceed_the_maximum_size()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GradLinkDbContext>();
        db.Notifications.AddRange(Enumerable.Range(0, NotificationService.MaxPageSize + 1)
            .Select(i => new Notification { UserId = employer.UserId, Message = $"Notification {i}" }));
        await db.SaveChangesAsync();

        var page = await scope.ServiceProvider.GetRequiredService<NotificationService>()
            .GetUserNotificationsAsync(employer.UserId, take: int.MaxValue);

        Assert.Equal(NotificationService.MaxPageSize, page.Count);
    }

    [Theory]
    [InlineData("?take=0")]
    [InlineData("?take=101")]
    [InlineData("?skip=-1")]
    public async Task Out_of_range_paging_is_rejected(string query)
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);

        var response = await employer.Client.GetAsync($"api/notifications{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Stored_timestamps_are_returned_in_utc()
    {
        var scenario = await factory.CreateApplicationAsync();

        using var notifications = JsonDocument.Parse(await scenario.Employer.Client.GetStringAsync("api/notifications"));
        using var applications = JsonDocument.Parse(await scenario.Graduate.Client.GetStringAsync("api/applications/my"));
        using var job = JsonDocument.Parse(await scenario.Graduate.Client.GetStringAsync($"api/jobs/{scenario.Job.Id}"));

        Assert.EndsWith("Z", notifications.RootElement[0].GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", applications.RootElement[0].GetProperty("appliedDate").GetString());
        Assert.EndsWith("Z", job.RootElement.GetProperty("deadline").GetString());
    }
}
