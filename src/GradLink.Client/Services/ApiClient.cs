using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GradLink.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace GradLink.Client.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly LocalStorageService _localStorage;
    private readonly NavigationManager _navManager;

    public ApiClient(HttpClient http, LocalStorageService localStorage, NavigationManager navManager)
    {
        _http = http;
        _localStorage = localStorage;
        _navManager = navManager;
    }

    private async Task PrepareBearerTokenAsync()
    {
        var token = await _localStorage.GetItemAsync("authToken");
        // Clear the header after logout; the HttpClient outlives the session and would keep sending the old token.
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }
    
    private void CheckUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _navManager.NavigateTo("/login");
        }
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.GetAsync(url);
            CheckUnauthorized(response);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();
            return default;
        }
        catch
        {
            return default;
        }
    }

    private async Task<TOut?> PostAsync<TIn, TOut>(string url, TIn payload)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.PostAsJsonAsync(url, payload);
            CheckUnauthorized(response);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<TOut>();
            return default;
        }
        catch
        {
            return default;
        }
    }

    private async Task<TOut?> PutAsync<TIn, TOut>(string url, TIn payload)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.PutAsJsonAsync(url, payload);
            CheckUnauthorized(response);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<TOut>();
            return default;
        }
        catch
        {
            return default;
        }
    }

    // Auth
    public Task<AuthResponseDto?> RegisterAsync(RegisterDto dto) =>
        PostAuthAsync("api/auth/register", dto, "Registration failed.");

    public Task<AuthResponseDto?> LoginAsync(LoginDto dto) =>
        PostAuthAsync("api/auth/login", dto, "Invalid email or password.");

    private async Task<AuthResponseDto?> PostAuthAsync<T>(string url, T dto, string defaultMessage)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(url, dto);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(content))
                return JsonSerializer.Deserialize<AuthResponseDto>(content, JsonOptions);

            return ParseErrorResponse(content, defaultMessage);
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
            var errorResult = JsonSerializer.Deserialize<AuthResponseDto>(content, JsonOptions);
            if (errorResult != null && !string.IsNullOrEmpty(errorResult.Message))
                return errorResult;
        } catch {}

        try {
            var problem = JsonSerializer.Deserialize<JsonElement>(content);
            
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
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(industry)) queryParams.Add($"industry={Uri.EscapeDataString(industry)}");
        if (!string.IsNullOrWhiteSpace(location)) queryParams.Add($"location={Uri.EscapeDataString(location)}");
        if (experienceLevel.HasValue) queryParams.Add($"experienceLevel={(int)experienceLevel.Value}");

        var url = "api/jobs" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
        return await GetAsync<List<JobListingDto>>(url);
    }

    public Task<JobListingDto?> GetJobByIdAsync(int id) => GetAsync<JobListingDto>($"api/jobs/{id}");
    public Task<JobListingDto?> PostJobAsync(CreateJobDto dto) => PostAsync<CreateJobDto, JobListingDto>("api/jobs", dto);
    public Task<JobListingDto?> UpdateJobAsync(int id, CreateJobDto dto) => PutAsync<CreateJobDto, JobListingDto>($"api/jobs/{id}", dto);

    public async Task<bool> ToggleJobStatusAsync(int id, bool isActive)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.PatchAsJsonAsync($"api/jobs/{id}/status", new UpdateJobStatusDto { IsActive = isActive });
            CheckUnauthorized(response);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.DeleteAsync($"api/jobs/{id}");
            CheckUnauthorized(response);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public Task<List<JobListingDto>?> GetEmployerJobsAsync() => GetAsync<List<JobListingDto>>("api/jobs/employer");

    // Applications
    public Task<List<ApplicationDto>?> GetGraduateApplicationsAsync() => GetAsync<List<ApplicationDto>>("api/applications/my");
    public Task<ApplicationDto?> ApplyAsync(CreateApplicationDto dto) => PostAsync<CreateApplicationDto, ApplicationDto>("api/applications", dto);
    public Task<List<ApplicationDto>?> GetJobApplicantsAsync(int jobId) => GetAsync<List<ApplicationDto>>($"api/applications/job/{jobId}");
    public Task<ApplicationDto?> UpdateApplicationStatusAsync(UpdateApplicationStatusDto dto) => PutAsync<UpdateApplicationStatusDto, ApplicationDto>("api/applications/status", dto);

    // Profiles
    public Task<GraduateProfileDto?> GetGraduateProfileAsync() => GetAsync<GraduateProfileDto>("api/profile/graduate");
    public Task<EmployerProfileDto?> GetEmployerProfileAsync() => GetAsync<EmployerProfileDto>("api/profile/employer");
    public Task<DashboardStatsDto?> GetDashboardStatsAsync() => GetAsync<DashboardStatsDto>("api/profile/stats");
    public Task<GraduateProfileDto?> UpdateGraduateProfileAsync(UpdateGraduateProfileDto dto) => PutAsync<UpdateGraduateProfileDto, GraduateProfileDto>("api/profile/graduate", dto);
    public Task<EmployerProfileDto?> UpdateEmployerProfileAsync(UpdateEmployerProfileDto dto) => PutAsync<UpdateEmployerProfileDto, EmployerProfileDto>("api/profile/employer", dto);

    // Notifications
    public Task<List<NotificationDto>?> GetNotificationsAsync(int skip = 0, int take = 20) => GetAsync<List<NotificationDto>>($"api/notifications?skip={skip}&take={take}");

    public async Task<bool> MarkNotificationAsReadAsync(int id)
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.PutAsync($"api/notifications/{id}/read", null);
            CheckUnauthorized(response);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync()
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.PutAsync("api/notifications/read-all", null);
            CheckUnauthorized(response);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        await PrepareBearerTokenAsync();
        try
        {
            var response = await _http.GetAsync("api/notifications/unread-count");
            CheckUnauthorized(response);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<int>();
            return 0;
        }
        catch { return 0; }
    }

    public async Task<bool> UploadCvAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        await PrepareBearerTokenAsync();
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/pdf" : file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await _http.PostAsync("api/files/cv", content);
            CheckUnauthorized(response);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
