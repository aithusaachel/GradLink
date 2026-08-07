using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GradLink.API.Data;
using GradLink.API.Hubs;
using GradLink.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add SQLite database
builder.Services.AddDbContext<GradLinkDbContext>(options =>
    options.UseSqlite("Data Source=gradlink.db"));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<GradLinkDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "GradLinkSuperSecretKeyThatIsLongEnough2025!";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GradLink",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GradLinkUsers",
        IssuerSigningKey = key,
        NameClaimType = ClaimTypes.NameIdentifier
    };

    // Allow SignalR to receive token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Add services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<NotificationService>();

// Add SignalR
builder.Services.AddSignalR();

// Add Controllers
builder.Services.AddControllers();

// Add OpenAPI
builder.Services.AddOpenApi();

// CORS - allow the Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5002",
                "http://localhost:5003",
                "https://localhost:7001",
                "http://localhost:5001",
                "http://localhost:5075",
                "http://localhost:5134",
                "https://localhost:7222")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    await SeedData.Initialize(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
