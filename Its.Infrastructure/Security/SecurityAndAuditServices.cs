using Its.Application.Interfaces;
using Its.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Its.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var secretKey = _config["Jwt:Key"] ?? "ITAssetAllocationSecretKeyMustBeAtLeast32BytesLong!";
        var issuer = _config["Jwt:Issuer"] ?? "ItsApi";
        var audience = _config["Jwt:Audience"] ?? "ItsApp";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("employee_id", user.EmployeeId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;

    public AuditService(IUnitOfWork uow, ICurrentUserService currentUserService)
    {
        _uow = uow;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(string action, string entityName, string? entityId, object? oldValue = null, object? newValue = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = _currentUserService.UserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                IpAddress = _currentUserService.IpAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.AuditLogs.AddAsync(log);
            await _uow.SaveChangesAsync();
        }
        catch
        {
            // Logging failure should not disrupt main transaction
        }
    }
}
