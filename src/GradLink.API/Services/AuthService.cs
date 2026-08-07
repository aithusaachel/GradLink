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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "GradLinkSuperSecretKeyThatIsLongEnough2025!"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("role", user.Role.ToString())
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "GradLink",
            audience: _configuration["Jwt:Audience"] ?? "GradLinkUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
