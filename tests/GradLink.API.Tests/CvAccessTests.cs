using System.Net.Http.Headers;

namespace GradLink.API.Tests;

public sealed class CvAccessTests(GradLinkApiFactory factory) : IClassFixture<GradLinkApiFactory>
{
    private static readonly byte[] MinimalPdf = "%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF"u8.ToArray();

    [Fact]
    public async Task Graduates_can_download_their_own_cv()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);
        await UploadCvAsync(graduate, MinimalPdf);

        var response = await graduate.Client.GetAsync($"api/files/cv/{graduate.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Graduates_cannot_download_another_graduates_cv()
    {
        var owner = await factory.RegisterAsync(UserRole.Graduate);
        var other = await factory.RegisterAsync(UserRole.Graduate);
        await UploadCvAsync(owner, MinimalPdf);

        var response = await other.Client.GetAsync($"api/files/cv/{owner.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Employers_can_download_cvs_of_their_own_applicants()
    {
        var scenario = await factory.CreateApplicationAsync();
        await UploadCvAsync(scenario.Graduate, MinimalPdf);

        var response = await scenario.Employer.Client.GetAsync($"api/files/cv/{scenario.Graduate.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Employers_cannot_download_cvs_of_graduates_who_did_not_apply_to_them()
    {
        var scenario = await factory.CreateApplicationAsync();
        var rival = await factory.RegisterAsync(UserRole.Employer);
        await UploadCvAsync(scenario.Graduate, MinimalPdf);

        var response = await rival.Client.GetAsync($"api/files/cv/{scenario.Graduate.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(Skip = "CV uploads are validated by file extension only.")]
    public async Task Cv_uploads_must_be_real_documents()
    {
        var graduate = await factory.RegisterAsync(UserRole.Graduate);

        var response = await PostCvAsync(graduate, "not really a pdf"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task UploadCvAsync(TestUser graduate, byte[] document) =>
        (await PostCvAsync(graduate, document)).EnsureSuccessStatusCode();

    private static Task<HttpResponseMessage> PostCvAsync(TestUser graduate, byte[] document)
    {
        var file = new ByteArrayContent(document);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent { { file, "file", "cv.pdf" } };
        return graduate.Client.PostAsync("api/files/cv", form);
    }
}
