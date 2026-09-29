namespace GradLink.API.Tests;

public sealed class ApplicationDeadlineTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Applications_are_refused_after_the_deadline()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        var job = await PostJobWithDeadlineAsync(employer, DateTime.UtcNow.AddDays(-1));

        var response = await graduate.ApplyAsync(job.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await employer.GetNotificationsAsync());
    }

    [Fact]
    public async Task Applications_are_accepted_on_the_deadline_day()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        // The web client posts date-only deadlines, i.e. midnight at the start of the deadline day.
        var job = await PostJobWithDeadlineAsync(employer, DateTime.UtcNow.Date);

        await graduate.ApplySuccessfullyAsync(job.Id);
    }

    private static async Task<JobListingDto> PostJobWithDeadlineAsync(TestUser employer, DateTime deadline)
    {
        var listing = ApiExtensions.NewListing();
        listing.Deadline = deadline;
        var response = await employer.Client.PostAsJsonAsync("api/jobs", listing);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobListingDto>())!;
    }
}
