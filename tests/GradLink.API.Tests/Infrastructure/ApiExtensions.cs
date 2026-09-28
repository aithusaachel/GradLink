using System.Net.Http.Headers;

namespace GradLink.API.Tests.Infrastructure;

public sealed record TestUser(string UserId, string Email, string FullName, string Token, HttpClient Client);

public sealed record HiringScenario(TestUser Employer, TestUser Graduate, JobListingDto Job, ApplicationDto Application);

public static class ApiExtensions
{
    public const string Password = "Test@12345";

    public static async Task<TestUser> RegisterAsync(this GradLinkApiFactory factory, UserRole role)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var fullName = $"{role} {suffix}";
        var email = $"{role.ToString().ToLowerInvariant()}.{suffix}@test.gradlink";

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterDto
        {
            Email = email,
            Password = Password,
            FullName = fullName,
            Role = role,
            CompanyName = role == UserRole.Employer ? $"{fullName} Ltd" : null
        });

        return await AuthenticateAsync(client, response, email);
    }

    public static async Task<TestUser> SignInAsync(this GradLinkApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginDto { Email = email, Password = password });
        return await AuthenticateAsync(client, response, email);
    }

    public static async Task<HiringScenario> CreateApplicationAsync(this GradLinkApiFactory factory)
    {
        var employer = await factory.RegisterAsync(UserRole.Employer);
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        var job = await employer.PostJobAsync();
        var application = await graduate.ApplySuccessfullyAsync(job.Id);
        return new HiringScenario(employer, graduate, job, application);
    }

    public static CreateJobDto NewListing(string? title = null) => new()
    {
        Title = title ?? $"Role {Guid.NewGuid().ToString("N")[..6]}",
        Description = "Integration test listing.",
        Location = "Accra, Ghana",
        Industry = "Technology",
        ExperienceLevel = ExperienceLevel.Entry,
        Deadline = DateTime.UtcNow.AddDays(30)
    };

    public static async Task<JobListingDto> PostJobAsync(this TestUser employer, string? title = null)
    {
        var response = await employer.Client.PostAsJsonAsync("api/jobs", NewListing(title));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobListingDto>())!;
    }

    public static Task<HttpResponseMessage> ApplyAsync(this TestUser graduate, int jobId, string? coverLetter = "I would like to apply.") =>
        graduate.Client.PostAsJsonAsync("api/applications", new CreateApplicationDto { JobListingId = jobId, CoverLetter = coverLetter });

    public static async Task<ApplicationDto> ApplySuccessfullyAsync(this TestUser graduate, int jobId)
    {
        var response = await graduate.ApplyAsync(jobId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDto>())!;
    }

    public static Task<HttpResponseMessage> SetStatusAsync(this TestUser employer, int applicationId, ApplicationStatus status) =>
        employer.Client.PutAsJsonAsync("api/applications/status", new UpdateApplicationStatusDto { ApplicationId = applicationId, NewStatus = status });

    public static async Task<List<NotificationDto>> GetNotificationsAsync(this TestUser user, string query = "") =>
        (await user.Client.GetFromJsonAsync<List<NotificationDto>>($"api/notifications{query}"))!;

    public static Task<int> GetUnreadCountAsync(this TestUser user) =>
        user.Client.GetFromJsonAsync<int>("api/notifications/unread-count");

    private static async Task<TestUser> AuthenticateAsync(HttpClient client, HttpResponseMessage response, string email)
    {
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new TestUser(auth.UserId, email, auth.FullName, auth.Token, client);
    }
}
