using System.Net.Http.Headers;
using System.Net.Http.Json;
using GradLink.Shared.DTOs;

namespace GradLink.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly LocalStorageService _localStorage;

    public ApiClient(HttpClient http, LocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task PrepareBearerTokenAsync()
    {
        var token = await _localStorage.GetItemAsync("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Auth
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        try 
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", dto);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(content))
                    return System.Text.Json.JsonSerializer.Deserialize<AuthResponseDto>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                return ParseErrorResponse(content, "Registration failed.");
            }
            return new AuthResponseDto { Success = false, Message = "Registration failed." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = "Network error: " + ex.Message };
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        try 
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", dto);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(content))
                    return System.Text.Json.JsonSerializer.Deserialize<AuthResponseDto>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                return ParseErrorResponse(content, "Invalid email or password.");
            }
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = "Network error: " + ex.Message };
        }
    }

    private AuthResponseDto ParseErrorResponse(string content, string defaultMessage)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new AuthResponseDto { Success = false, Message = defaultMessage };

        try {
            var errorResult = System.Text.Json.JsonSerializer.Deserialize<AuthResponseDto>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (errorResult != null && !string.IsNullOrEmpty(errorResult.Message))
                return errorResult;
        } catch {}

        try {
            var problem = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
            
            var msg = "";
            if (problem.TryGetProperty("title", out var titleProp))
                msg = titleProp.GetString();
                
            if (problem.TryGetProperty("errors", out var errorsProp))
            {
                var errorList = new List<string>();
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    foreach (var val in prop.Value.EnumerateArray())
                    {
                        errorList.Add(val.GetString() ?? "");
                    }
                }
                if (errorList.Any())
                    msg = string.Join("; ", errorList);
            }
            
            return new AuthResponseDto { Success = false, Message = !string.IsNullOrEmpty(msg) ? msg : defaultMessage };
        } catch {}

        return new AuthResponseDto { Success = false, Message = defaultMessage };
    }

    public Uri? BaseAddress => _http.BaseAddress;

    public string GetHubUrl(string relativePath = "hubs/notifications") =>
        new Uri(_http.BaseAddress ?? new Uri("http://localhost:5150"), relativePath.TrimStart('/')).ToString();

    public string GetCvDownloadUrl(string userId) =>
        new Uri(_http.BaseAddress ?? new Uri("http://localhost:5150"), $"api/files/cv/{userId}").ToString();

    // Jobs
    public async Task<List<JobListingDto>?> GetJobsAsync(
        string? search = null,
        string? industry = null,
        string? location = null,
        GradLink.Shared.Enums.ExperienceLevel? experienceLevel = null)
    {
        await PrepareBearerTokenAsync();
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(industry)) queryParams.Add($"industry={Uri.EscapeDataString(industry)}");
        if (!string.IsNullOrWhiteSpace(location)) queryParams.Add($"location={Uri.EscapeDataString(location)}");
        if (experienceLevel.HasValue) queryParams.Add($"experienceLevel={(int)experienceLevel.Value}");

        var url = "api/jobs" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
        return await _http.GetFromJsonAsync<List<JobListingDto>>(url);
    }

    public async Task<JobListingDto?> GetJobByIdAsync(int id)
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<JobListingDto>($"api/jobs/{id}");
    }

    public async Task<JobListingDto?> PostJobAsync(CreateJobDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PostAsJsonAsync("api/jobs", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<JobListingDto>();
        return null;
    }

    public async Task<JobListingDto?> UpdateJobAsync(int id, CreateJobDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/jobs/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<JobListingDto>();
        return null;
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.DeleteAsync($"api/jobs/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<JobListingDto>?> GetEmployerJobsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<JobListingDto>>("api/jobs/employer");
    }

    // Applications
    public async Task<List<ApplicationDto>?> GetGraduateApplicationsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<ApplicationDto>>("api/applications/my");
    }

    public async Task<ApplicationDto?> ApplyAsync(CreateApplicationDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PostAsJsonAsync("api/applications", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ApplicationDto>();
        return null;
    }

    public async Task<List<ApplicationDto>?> GetJobApplicantsAsync(int jobId)
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<ApplicationDto>>($"api/applications/job/{jobId}");
    }

    public async Task<ApplicationDto?> UpdateApplicationStatusAsync(UpdateApplicationStatusDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsJsonAsync("api/applications/status", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ApplicationDto>();
        return null;
    }

    // Profiles
    public async Task<GraduateProfileDto?> GetGraduateProfileAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<GraduateProfileDto>("api/profile/graduate");
    }

    public async Task<EmployerProfileDto?> GetEmployerProfileAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<EmployerProfileDto>("api/profile/employer");
    }

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<DashboardStatsDto>("api/profile/stats");
    }

    public async Task<GraduateProfileDto?> UpdateGraduateProfileAsync(UpdateGraduateProfileDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsJsonAsync("api/profile/graduate", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GraduateProfileDto>();
        return null;
    }

    public async Task<EmployerProfileDto?> UpdateEmployerProfileAsync(UpdateEmployerProfileDto dto)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsJsonAsync("api/profile/employer", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<EmployerProfileDto>();
        return null;
    }

    // Notifications
    public async Task<List<NotificationDto>?> GetNotificationsAsync(int skip = 0, int take = 20)
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<NotificationDto>>($"api/notifications?skip={skip}&take={take}");
    }

    public async Task<bool> MarkNotificationAsReadAsync(int id)
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsync($"api/notifications/{id}/read", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync()
    {
        await PrepareBearerTokenAsync();
        var response = await _http.PutAsync("api/notifications/read-all", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        await PrepareBearerTokenAsync();
        try
        {
            return await _http.GetFromJsonAsync<int>("api/notifications/unread-count");
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> UploadCvAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        await PrepareBearerTokenAsync();
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream(5 * 1024 * 1024);
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/pdf" : file.ContentType);
        content.Add(streamContent, "file", file.Name);

        var response = await _http.PostAsync("api/files/cv", content);
        return response.IsSuccessStatusCode;
    }
}

