using System.Net.Http.Headers;
using System.Text;
using GradLink.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradLink.API.Tests;

public sealed class CvStorageTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    private string CvDirectory =>
        Path.Combine(factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "Uploads", "CVs");

    [Fact]
    public async Task Reuploading_replaces_the_previous_cv()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);

        // Back-to-back uploads land in the same second.
        (await UploadAsync(graduate, "first version")).EnsureSuccessStatusCode();
        (await UploadAsync(graduate, "second version")).EnsureSuccessStatusCode();

        var download = await graduate.Client.GetAsync($"api/files/cv/{graduate.UserId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("second version", await download.Content.ReadAsStringAsync());
        Assert.Single(Directory.GetFiles(CvDirectory, $"{graduate.UserId}_*"));
    }

    [Fact]
    public async Task Unsupported_file_types_are_rejected()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);

        var response = await UploadAsync(graduate, "#!/bin/sh", "cv.sh");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cvs_stored_with_an_absolute_path_still_download()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        (await UploadAsync(graduate, "legacy cv")).EnsureSuccessStatusCode();

        // Older databases stored the absolute path on disk rather than just the file name.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GradLinkDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == graduate.UserId);
            user.CvFilePath = Path.Combine(CvDirectory, user.CvFilePath!);
            await db.SaveChangesAsync();
        }

        var download = await graduate.Client.GetAsync($"api/files/cv/{graduate.UserId}");

        Assert.Equal("legacy cv", await download.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> UploadAsync(TestUser graduate, string content, string fileName = "cv.pdf")
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return graduate.Client.PostAsync("api/files/cv", form);
    }
}
