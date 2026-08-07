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

    // Jobs
    public async Task<List<JobListingDto>?> GetJobsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<JobListingDto>>("api/jobs");
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

    public async Task<List<JobListingDto>?> GetEmployerJobsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<JobListingDto>>("api/jobs/employer");
    }

    // Applications
    public async Task<List<ApplicationDto>?> GetGraduateApplicationsAsync()
    {
        await PrepareBearerTokenAsync();
        return await _http.GetFromJsonAsync<List<ApplicationDto>>("api/applications/graduate");
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
}
