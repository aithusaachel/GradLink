namespace GradLink.API.Tests;

public sealed class JobListingTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Job_board_search_matches_titles_and_counts_applications()
    {
        var scenario = await factory.CreateApplicationAsync();

        var results = await scenario.Graduate.Client.GetFromJsonAsync<List<JobListingDto>>(
            $"api/jobs?search={Uri.EscapeDataString(scenario.Job.Title.ToUpperInvariant())}");

        var listing = Assert.Single(results!);
        Assert.Equal(scenario.Job.Id, listing.Id);
        Assert.Equal(scenario.Job.CompanyName, listing.CompanyName);
        Assert.Equal(1, listing.ApplicationCount);
    }

    [Fact]
    public async Task Employers_can_update_and_delete_only_their_own_listings()
    {
        var owner = await factory.RegisterAsync(UserRole.Employer);
        var rival = await factory.RegisterAsync(UserRole.Employer);
        var job = await owner.PostJobAsync();

        var rivalUpdate = await rival.Client.PutAsJsonAsync($"api/jobs/{job.Id}", ApiExtensions.NewListing("Hijacked"));
        var rivalDelete = await rival.Client.DeleteAsync($"api/jobs/{job.Id}");
        var ownerUpdate = await owner.Client.PutAsJsonAsync($"api/jobs/{job.Id}", ApiExtensions.NewListing("Renamed"));

        Assert.Equal(HttpStatusCode.NotFound, rivalUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rivalDelete.StatusCode);
        Assert.Equal("Renamed", (await ownerUpdate.Content.ReadFromJsonAsync<JobListingDto>())!.Title);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"api/jobs/{job.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Client.GetAsync($"api/jobs/{job.Id}")).StatusCode);
    }

    [Fact]
    public async Task Employer_stats_count_listings_and_applicants()
    {
        var scenario = await factory.CreateApplicationAsync();
        var second = await scenario.Employer.PostJobAsync();
        await (await factory.RegisterAsync(UserRole.Graduate)).ApplySuccessfullyAsync(second.Id);

        var stats = await scenario.Employer.Client.GetFromJsonAsync<DashboardStatsDto>("api/profile/stats");

        Assert.Equal(2, stats!.TotalJobsPosted);
        Assert.Equal(2, stats.ActiveJobs);
        Assert.Equal(2, stats.TotalApplicants);
        Assert.Equal(2, stats.NewApplicantsToday);
    }
}
