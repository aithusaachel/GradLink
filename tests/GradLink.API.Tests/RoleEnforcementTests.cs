namespace GradLink.API.Tests;

public sealed class RoleEnforcementTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    [Fact]
    public async Task Graduates_cannot_post_jobs()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);

        var response = await graduate.Client.PostAsJsonAsync("api/jobs", ApiExtensions.NewListing());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Employers_cannot_apply_for_jobs()
    {
        var owner = await factory.RegisterAsync(UserRole.Employer);
        var other = await factory.RegisterAsync(UserRole.Employer);
        var job = await owner.PostJobAsync();

        var response = await other.ApplyAsync(job.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
