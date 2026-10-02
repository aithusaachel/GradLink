using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GradLink.API.Tests.Infrastructure;

public sealed class GradLinkApiFactory : WebApplicationFactory<Program>
{
    // Serves as the content root, so the database and uploaded CVs never land in the repository.
    private readonly string _root = Directory.CreateTempSubdirectory("gradlink-tests-").FullName;

    public HubConnectionTracker HubConnections { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(_root);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(_root, "gradlink.db")}"
        }));
        // Program.cs reads Jwt:Key before the host is built, which only host-level settings reach.
        builder.UseSetting("Jwt:Key", "gradlink-integration-tests-signing-key-0123456789");

        builder.ConfigureTestServices(services =>
        {
            services.Configure<HubOptions>(options => options.AddFilter(HubConnections));
            services.AddControllers().AddApplicationPart(typeof(GradLinkApiFactory).Assembly);
            services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, FixResponseBodyWriterStartupFilter>();
        });
    }

    private sealed class FixResponseBodyWriterStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(nextMiddleware =>
                {
                    return async context =>
                    {
                        var bodyFeature = new Microsoft.AspNetCore.Http.StreamResponseBodyFeature(context.Response.Body);
                        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>(bodyFeature);
                        await nextMiddleware(context);
                    };
                });
                next(app);
            };
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
