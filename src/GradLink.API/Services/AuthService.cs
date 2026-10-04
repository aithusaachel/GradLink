using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using GradLink.API.Data;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (!Enum.IsDefined(typeof(UserRole), dto.Role))
            return new AuthResponseDto { Success = false, Message = "Invalid account role selected." };

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
            return new AuthResponseDto { Success = false, Message = "Email, password, and full name are required." };

        dto.Email = dto.Email.Trim();
        dto.FullName = dto.FullName.Trim();

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return new AuthResponseDto { Success = false, Message = "An account with this email already exists." };

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            Role = dto.Role,
            EmailConfirmed = true,
            University = dto.University,
            Degree = dto.Degree,
            GraduationYear = dto.GraduationYear,
            CompanyName = dto.CompanyName,
            Industry = dto.Industry
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResponseDto { Success = false, Message = errors };
        }

        var token = GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Token = token,
            Message = "Registration successful.",
            Role = user.Role,
            UserId = user.Id,
            FullName = user.FullName
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        dto.Email = dto.Email.Trim();

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };

        var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!validPassword)
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };

        var token = GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Token = token,
            Message = "Login successful.",
            Role = user.Role,
            UserId = user.Id,
            FullName = user.FullName
        };
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("JWT Key must be at least 32 bytes long for HS256 signing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString())
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "GradLink",
            audience: _configuration["Jwt:Audience"] ?? "GradLinkUsers",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
