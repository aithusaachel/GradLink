namespace GradLink.API.Tests;

public sealed class HiringWorkflowTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Employer_posts_graduate_applies_employer_reviews_and_updates_status()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        await using var employerFeed = await NotificationListener.ConnectAsync(factory, employer.Token);
        await using var graduateFeed = await NotificationListener.ConnectAsync(factory, graduate.Token);

        var job = await employer.PostJobAsync("Junior Backend Engineer");
        var board = await graduate.Client.GetFromJsonAsync<List<JobListingDto>>("api/jobs");
        Assert.Contains(board!, listing => listing.Id == job.Id);

        var applied = await graduate.ApplyAsync(job.Id, "I build reliable APIs.");
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
        Assert.Contains(graduate.FullName, (await employerFeed.NextAsync()).Message);

        var applicants = await employer.Client.GetFromJsonAsync<List<ApplicationDto>>($"api/applications/job/{job.Id}");
        var applicant = Assert.Single(applicants!);
        Assert.Equal(graduate.UserId, applicant.GraduateId);
        Assert.Equal("I build reliable APIs.", applicant.CoverLetter);
        Assert.Equal(ApplicationStatus.Pending, applicant.Status);

        (await employer.SetStatusAsync(applicant.Id, ApplicationStatus.Shortlisted)).EnsureSuccessStatusCode();
        Assert.Contains(nameof(ApplicationStatus.Shortlisted), (await graduateFeed.NextAsync()).Message);

        var mine = await graduate.Client.GetFromJsonAsync<List<ApplicationDto>>("api/applications/my");
        Assert.Equal(ApplicationStatus.Shortlisted, Assert.Single(mine!).Status);
        Assert.Equal(1, (await graduate.Client.GetFromJsonAsync<DashboardStatsDto>("api/profile/stats"))!.ShortlistedApplications);
        Assert.Equal(1, (await employer.Client.GetFromJsonAsync<DashboardStatsDto>("api/profile/stats"))!.TotalApplicants);

        var repeat = await graduate.ApplyAsync(job.Id);
        Assert.False(repeat.IsSuccessStatusCode);
        Assert.True((int)repeat.StatusCode < 500);
        Assert.Single(await employer.GetNotificationsAsync());
    }

    [Fact]
    public async Task Other_employers_cannot_see_or_change_a_listings_applications()
    {
        var scenario = await factory.CreateApplicationAsync();
        var rival = await factory.RegisterAsync(UserRole.Employer);

        var listing = await rival.Client.GetAsync($"api/applications/job/{scenario.Job.Id}");
        var change = await rival.SetStatusAsync(scenario.Application.Id, ApplicationStatus.Rejected);

        Assert.InRange((int)listing.StatusCode, 200, 499);
        if (listing.IsSuccessStatusCode)
            Assert.Empty((await listing.Content.ReadFromJsonAsync<List<ApplicationDto>>())!);
        Assert.InRange((int)change.StatusCode, 400, 499);
        Assert.Empty(await scenario.Graduate.GetNotificationsAsync());
    }

    [Fact]
    public async Task Undefined_application_statuses_are_rejected()
    {
        var scenario = await factory.CreateApplicationAsync();

        var response = await scenario.Employer.SetStatusAsync(scenario.Application.Id, (ApplicationStatus)99);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
