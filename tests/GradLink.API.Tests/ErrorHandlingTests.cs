using System.Text;
using System.Text.Json;

namespace GradLink.API.Tests;

public sealed class ErrorHandlingTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task Unknown_routes_return_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unauthenticated_requests_return_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unhandled_exceptions_return_problem_details_without_a_stack_trace()
    {
        var response = await factory.CreateClient().GetAsync("test/faults/unhandled");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(500, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain($"{nameof(FaultInjectionController)}.", body);
    }

    [Fact]
    public async Task Unique_constraint_violations_return_a_conflict()
    {
        var response = await factory.CreateClient().GetAsync("test/faults/unique-violation");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Malformed_json_returns_a_validation_problem()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);

        var response = await graduate.Client.PostAsync("api/applications", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Concurrent_duplicate_applications_are_refused_like_a_repeat_application()
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        var job = await employer.PostJobAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => graduate.ApplyAsync(job.Id)));
        var repeat = await graduate.ApplyAsync(job.Id);

        Assert.Single(responses, response => response.IsSuccessStatusCode);
        Assert.InRange((int)repeat.StatusCode, 400, 499);
        Assert.All(responses.Where(response => !response.IsSuccessStatusCode), response =>
        {
            Assert.Equal(repeat.StatusCode, response.StatusCode);
            Assert.Equal(repeat.Content.Headers.ContentType?.MediaType, response.Content.Headers.ContentType?.MediaType);
        });
        Assert.Single((await graduate.Client.GetFromJsonAsync<List<ApplicationDto>>("api/applications/my"))!);
        Assert.Single(await employer.GetNotificationsAsync());
    }
}
