namespace GradLink.API.Tests;

public sealed class RealTimeNotificationTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Hub_rejects_connections_without_a_token()
    {
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => NotificationListener.ConnectAsync(factory, accessToken: null));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
    }

    [Fact]
    public async Task Hub_accepts_the_access_token_query_parameter()
    {
        var scenario = await factory.CreateApplicationAsync();
        await using var feed = await NotificationListener.ConnectAsync(factory, scenario.Graduate.Token, tokenInQueryString: true);

        (await scenario.Employer.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Reviewed)).EnsureSuccessStatusCode();

        Assert.Contains(nameof(ApplicationStatus.Reviewed), (await feed.NextAsync()).Message);
    }

    [Fact]
    public async Task Graduates_receive_status_changes_as_they_happen()
    {
        var scenario = await factory.CreateApplicationAsync();
        await using var feed = await NotificationListener.ConnectAsync(factory, scenario.Graduate.Token);

        (await scenario.Employer.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Accepted)).EnsureSuccessStatusCode();

        var pushed = await feed.NextAsync();
        var stored = Assert.Single(await scenario.Graduate.GetNotificationsAsync());
        Assert.Equal(stored.Id, pushed.Id);
        Assert.Equal(stored.Message, pushed.Message);
        Assert.Contains(nameof(ApplicationStatus.Accepted), pushed.Message);
    }

    [Fact]
    public async Task Employers_receive_new_applications_as_they_happen()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        var job = await employer.PostJobAsync();
        await using var feed = await NotificationListener.ConnectAsync(factory, employer.Token);

        await graduate.ApplySuccessfullyAsync(job.Id);

        var pushed = await feed.NextAsync();
        Assert.Contains(graduate.FullName, pushed.Message);
        Assert.Contains(job.Title, pushed.Message);
    }

    [Fact]
    public async Task Pushes_reach_only_their_recipient()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var job = await employer.PostJobAsync();
        var first = await factory.RegisterAsync(UserRole.Graduate);
        var second = await factory.RegisterAsync(UserRole.Graduate);
        var firstApplication = await first.ApplySuccessfullyAsync(job.Id);
        var secondApplication = await second.ApplySuccessfullyAsync(job.Id);
        await using var firstFeed = await NotificationListener.ConnectAsync(factory, first.Token);
        await using var secondFeed = await NotificationListener.ConnectAsync(factory, second.Token);

        (await employer.SetStatusAsync(firstApplication.Id, ApplicationStatus.Rejected)).EnsureSuccessStatusCode();
        (await employer.SetStatusAsync(secondApplication.Id, ApplicationStatus.Shortlisted)).EnsureSuccessStatusCode();

        Assert.Contains(nameof(ApplicationStatus.Rejected), (await firstFeed.NextAsync()).Message);
        Assert.Contains(nameof(ApplicationStatus.Shortlisted), (await secondFeed.NextAsync()).Message);
    }
}
