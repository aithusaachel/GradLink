using GradLink.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradLink.API.Tests;

public sealed class SeedDataTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    public static TheoryData<string, string> DocumentedAccounts => new()
    {
        { "alice@gradlink.com", "Password@123" },
        { "techcorp@gradlink.com", "Password@123" }
    };

    [Theory]
    [MemberData(nameof(DocumentedAccounts))]
    public async Task Documented_sample_accounts_can_sign_in(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("api/auth/login", new LoginDto { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_applications_have_a_matching_notification_history()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GradLinkDbContext>();
        var applications = await db.JobApplications.Include(a => a.JobListing).Include(a => a.Graduate).ToListAsync();
        var notifications = await db.Notifications.ToListAsync();

        Assert.NotEmpty(applications);
        foreach (var application in applications)
        {
            Assert.Contains(notifications, n => n.UserId == application.JobListing.EmployerId
                && n.Message.Contains(application.Graduate.FullName)
                && n.Message.Contains(application.JobListing.Title));

            if (application.Status != ApplicationStatus.Pending)
            {
                Assert.Contains(notifications, n => n.UserId == application.GraduateId
                    && n.Message.Contains(application.JobListing.Title)
                    && n.Message.Contains(application.Status.ToString()));
            }
        }
    }

    [Fact]
    public async Task Seeded_history_is_visible_through_the_api()
    {
        var graduate = await factory.SignInAsync("john.doe@gradlink.com", "Password@123");

        Assert.NotEmpty(await graduate.GetNotificationsAsync());
        Assert.True(await graduate.GetUnreadCountAsync() > 0);
    }

    [Fact]
    public async Task Seeding_an_already_seeded_database_changes_nothing()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GradLinkDbContext>();
        var before = await CountRowsAsync(db);

        await SeedData.Initialize(scope.ServiceProvider);

        Assert.Equal(before, await CountRowsAsync(db));
    }

    [Fact]
    public async Task A_failed_seed_leaves_the_database_empty()
    {
        var root = Directory.CreateTempSubdirectory("gradlink-seed-").FullName;
        var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<GradLinkDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(root, "gradlink.db")}"));
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<GradLinkDbContext>();
        services.AddScoped<IUserValidator<ApplicationUser>>(_ => new RejectingUserValidator("john.doe@gradlink.com"));

        try
        {
            await using var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SeedData.Initialize(scope.ServiceProvider));
                Assert.Contains("john.doe@gradlink.com", error.Message);
            }

            using (var scope = provider.CreateScope())
            {
                Assert.Equal((0, 0, 0, 0), await CountRowsAsync(scope.ServiceProvider.GetRequiredService<GradLinkDbContext>()));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RejectingUserValidator(string email) : IUserValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(user.Email == email
                ? IdentityResult.Failed(new IdentityError { Description = "Rejected for this test." })
                : IdentityResult.Success);
    }

    private static async Task<(int Users, int Jobs, int Applications, int Notifications)> CountRowsAsync(GradLinkDbContext db) =>
        (await db.Users.CountAsync(), await db.JobListings.CountAsync(), await db.JobApplications.CountAsync(), await db.Notifications.CountAsync());
}
