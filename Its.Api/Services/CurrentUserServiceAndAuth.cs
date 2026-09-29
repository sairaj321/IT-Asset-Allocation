using Its.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Its.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var val = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return long.TryParse(val, out var id) ? id : null;
        }
    }

    public long? EmployeeId
    {
        get
        {
            var val = User?.FindFirst("employee_id")?.Value;
            return long.TryParse(val, out var id) ? id : null;
        }
    }

    public string? Username => User?.Identity?.Name;

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public IReadOnlyList<string> Permissions => User?.FindAll("permission").Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("ITS_MANAGER"))
        {
            // Manager has superuser permissions in ITS module
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasPermission = context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
