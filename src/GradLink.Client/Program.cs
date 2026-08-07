using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GradLink.Client;
using Microsoft.AspNetCore.Components.Authorization;
using GradLink.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5150") });
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
