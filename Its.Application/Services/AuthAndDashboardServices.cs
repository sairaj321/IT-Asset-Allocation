using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Interfaces;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Domain.Exceptions;

namespace Its.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AuthService(
        IUnitOfWork uow,
        IJwtTokenService jwtService,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _uow = uow;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();
        var allUsers = await _uow.Users.GetAllAsync();
        var allEmployees = await _uow.Employees.GetAllAsync();
        var empDict = allEmployees.ToDictionary(e => e.EmployeeId);

        var user = allUsers.FirstOrDefault(u => 
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) ||
            (empDict.TryGetValue(u.EmployeeId, out var emp) && string.Equals(emp.EmployeeCode, username, StringComparison.OrdinalIgnoreCase)));

        if (user == null)
        {
            await _auditService.LogAsync("Login Failed", "User", null, null, new { Username = username, Reason = "User not found" });
            return Result<LoginResponse>.Failure("Invalid username or password.", "AUTH_FAILED");
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync("Login Failed", "User", user.UserId.ToString(), null, new { Username = username, Reason = "Account inactive" });
            return Result<LoginResponse>.Failure("Your account has been deactivated. Please contact your administrator.", "ACCOUNT_INACTIVE");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remaining = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            return Result<LoginResponse>.Failure($"Account is temporarily locked out. Try again in {remaining} minutes.", "ACCOUNT_LOCKED");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginCount = 0;
            }
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            await _auditService.LogAsync("Login Failed", "User", user.UserId.ToString(), null, new { Username = username, Reason = "Invalid password", FailedAttempts = user.FailedLoginCount });

            return Result<LoginResponse>.Failure("Invalid username or password.", "AUTH_FAILED");
        }

        // Reset failed login count
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        _uow.Users.Update(user);

        // Fetch roles and permissions
        var userRoles = await _uow.UserRoles.FindAsync(ur => ur.UserId == user.UserId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = (await _uow.Roles.FindAsync(r => roleIds.Contains(r.RoleId))).Select(r => r.Name).ToList();


        Console.WriteLine("========== USER ROLES ==========");
        Console.WriteLine($"UserId: {user.UserId}");
        Console.WriteLine($"Username: {user.Username}");

        foreach (var role in roles)
        {
            Console.WriteLine($"ROLE = [{role}]");
        }

        Console.WriteLine("================================");

        var rolePermissions = await _uow.RolePermissions.FindAsync(rp => roleIds.Contains(rp.RoleId));
        var permIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = (await _uow.Permissions.FindAsync(p => permIds.Contains(p.PermissionId))).Select(p => p.Code).Distinct().ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _jwtService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity);
        await _uow.SaveChangesAsync();

        var employee = await _uow.Employees.GetByIdAsync(user.EmployeeId);
        string? deptName = null;
        if (employee?.DepartmentId.HasValue == true)
        {
            var dept = await _uow.Departments.GetByIdAsync(employee.DepartmentId.Value);
            deptName = dept?.Name;
        }

        var userDto = new UserDto
        {
            UserId = user.UserId,
            EmployeeId = user.EmployeeId,
            Username = user.Username,
            EmployeeCode = employee?.EmployeeCode ?? string.Empty,
            FullName = employee?.FullName ?? user.Username,
            Email = employee?.Email ?? string.Empty,
            Designation = employee?.Designation,
            DepartmentName = deptName,
            MustChangePassword = user.MustChangePassword,
            Roles = roles,
            Permissions = permissions
        };

        await _auditService.LogAsync("Login Success", "User", user.UserId.ToString(), null, new { user.Username, Roles = roles });

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            User = userDto
        });
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var tokenHash = _jwtService.HashToken(request.RefreshToken);
        var token = await _uow.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token == null || !token.IsActive)
        {
            return Result<LoginResponse>.Failure("Invalid or expired refresh token.", "INVALID_TOKEN");
        }

        // Revoke current token (rotation)
        token.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(token);

        var user = await _uow.Users.GetByIdAsync(token.UserId);
        if (user == null || !user.IsActive)
        {
            return Result<LoginResponse>.Failure("User is no longer active.", "USER_INACTIVE");
        }

        var userRoles = await _uow.UserRoles.FindAsync(ur => ur.UserId == user.UserId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = (await _uow.Roles.FindAsync(r => roleIds.Contains(r.RoleId))).Select(r => r.Name).ToList();

        var rolePermissions = await _uow.RolePermissions.FindAsync(rp => roleIds.Contains(rp.RoleId));
        var permIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = (await _uow.Permissions.FindAsync(p => permIds.Contains(p.PermissionId))).Select(p => p.Code).Distinct().ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtService.HashToken(rawRefreshToken);

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _uow.SaveChangesAsync();

        var employee = await _uow.Employees.GetByIdAsync(user.EmployeeId);
        string? deptName = null;
        if (employee?.DepartmentId.HasValue == true)
        {
            var dept = await _uow.Departments.GetByIdAsync(employee.DepartmentId.Value);
            deptName = dept?.Name;
        }

        var userDto = new UserDto
        {
            UserId = user.UserId,
            EmployeeId = user.EmployeeId,
            Username = user.Username,
            EmployeeCode = employee?.EmployeeCode ?? string.Empty,
            FullName = employee?.FullName ?? user.Username,
            Email = employee?.Email ?? string.Empty,
            Designation = employee?.Designation,
            DepartmentName = deptName,
            MustChangePassword = user.MustChangePassword,
            Roles = roles,
            Permissions = permissions
        };

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            User = userDto
        });
    }

    public async Task<Result> LogoutAsync()
    {
        if (_currentUserService.UserId.HasValue)
        {
            var activeTokens = await _uow.RefreshTokens.FindAsync(t => t.UserId == _currentUserService.UserId.Value && t.RevokedAt == null);
            foreach (var t in activeTokens)
            {
                t.RevokedAt = DateTime.UtcNow;
                _uow.RefreshTokens.Update(t);
            }
            await _uow.SaveChangesAsync();
            await _auditService.LogAsync("Logout", "User", _currentUserService.UserId.Value.ToString());
        }

        return Result.Success("Successfully logged out.");
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Result.Failure("User not authenticated.", "UNAUTHORIZED");
        }

        var user = await _uow.Users.GetByIdAsync(_currentUserService.UserId.Value);
        if (user == null)
        {
            return Result.Failure("User not found.", "NOT_FOUND");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Current password is incorrect.", "INVALID_PASSWORD");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Change Password", "User", user.UserId.ToString());

        return Result.Success("Password changed successfully.");
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Result<UserDto>.Failure("User not authenticated.", "UNAUTHORIZED");
        }

        var user = await _uow.Users.GetByIdAsync(_currentUserService.UserId.Value);
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        var employee = await _uow.Employees.GetByIdAsync(user.EmployeeId);
        string? deptName = null;
        if (employee?.DepartmentId.HasValue == true)
        {
            var dept = await _uow.Departments.GetByIdAsync(employee.DepartmentId.Value);
            deptName = dept?.Name;
        }

        var userRoles = await _uow.UserRoles.FindAsync(ur => ur.UserId == user.UserId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = (await _uow.Roles.FindAsync(r => roleIds.Contains(r.RoleId))).Select(r => r.Name).ToList();

        var rolePermissions = await _uow.RolePermissions.FindAsync(rp => roleIds.Contains(rp.RoleId));
        var permIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = (await _uow.Permissions.FindAsync(p => permIds.Contains(p.PermissionId))).Select(p => p.Code).Distinct().ToList();

        return Result<UserDto>.Success(new UserDto
        {
            UserId = user.UserId,
            EmployeeId = user.EmployeeId,
            Username = user.Username,
            EmployeeCode = employee?.EmployeeCode ?? string.Empty,
            FullName = employee?.FullName ?? user.Username,
            Email = employee?.Email ?? string.Empty,
            Designation = employee?.Designation,
            DepartmentName = deptName,
            MustChangePassword = user.MustChangePassword,
            Roles = roles,
            Permissions = permissions
        });
    }
}

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(IUnitOfWork uow, ICurrentUserService currentUserService)
    {
        _uow = uow;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync()
    {
        var empId = _currentUserService.EmployeeId;
        if (!empId.HasValue)
        {
            return Result<EmployeeDashboardDto>.Failure("Employee profile not associated with user.", "NO_EMPLOYEE");
        }

        var requests = (await _uow.AssetRequests.FindAsync(r => r.EmployeeId == empId.Value))
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToList();

        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);

        var requestDtos = requests.Select(r => new AssetRequestDto
        {
            RequestId = r.RequestId,
            RequestNumber = r.RequestNumber,
            EmployeeId = r.EmployeeId,
            AssetCategoryId = r.AssetCategoryId,
            CategoryName = categories.TryGetValue(r.AssetCategoryId, out var name) ? name : "Unknown",
            Reason = r.Reason,
            RequiredDate = r.RequiredDate,
            Priority = r.Priority,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        var activeAllocations = await _uow.AssetAllocations.FindAsync(a => a.EmployeeId == empId.Value && a.AllocationStatus == AllocationStatus.Active);
        var assetIds = activeAllocations.Select(a => a.AssetId).ToList();
        var assets = await _uow.Assets.FindAsync(a => assetIds.Contains(a.AssetId));

        var assetDtos = assets.Select(a =>
        {
            var alloc = activeAllocations.FirstOrDefault(x => x.AssetId == a.AssetId);
            return new AssetDto
            {
                AssetId = a.AssetId,
                AssetCode = a.AssetCode,
                AssetName = a.AssetName,
                AssetCategoryId = a.AssetCategoryId,
                CategoryName = categories.TryGetValue(a.AssetCategoryId, out var cname) ? cname : "Unknown",
                SerialNumber = a.SerialNumber,
                Brand = a.Brand,
                Model = a.Model,
                PurchaseDate = a.PurchaseDate,
                PurchaseCost = a.PurchaseCost,
                WarrantyStartDate = a.WarrantyStartDate,
                WarrantyExpiryDate = a.WarrantyExpiryDate,
                Status = a.Status,
                Condition = a.Condition,
                Location = a.Location,
                CurrentEmployeeId = a.CurrentEmployeeId,
                AllocationDate = alloc?.AllocationDate,
                ExpectedReturnDate = alloc?.ExpectedReturnDate,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt
            };
        }).ToList();

        var pendingCount = await _uow.AssetRequests.CountAsync(r => r.EmployeeId == empId.Value && 
            (r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview || r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking));

        return Result<EmployeeDashboardDto>.Success(new EmployeeDashboardDto
        {
            MyRequests = requestDtos,
            MyAssets = assetDtos,
            ActiveAssetsCount = assetDtos.Count,
            PendingRequestsCount = pendingCount
        });
    }

    public async Task<Result<ManagerDashboardDto>> GetManagerDashboardAsync()
    {
        var totalAssets = await _uow.Assets.CountAsync(a => a.IsActive);
        var assigned = await _uow.Assets.CountAsync(a => a.IsActive && a.Status == AssetStatus.Assigned);
        var available = await _uow.Assets.CountAsync(a => a.IsActive && a.Status == AssetStatus.Available);
        var underRepair = await _uow.Assets.CountAsync(a => a.IsActive && (a.Status == AssetStatus.UnderMaintenance || a.Status == AssetStatus.InRepair));
        var damaged = await _uow.Assets.CountAsync(a => a.IsActive && a.Status == AssetStatus.Damaged);

        var execRole = await _uow.Roles.FirstOrDefaultAsync(r => r.Name == "ITS_EXECUTIVE");
        int execCount = 0;
        if (execRole != null)
        {
            execCount = await _uow.UserRoles.CountAsync(ur => ur.RoleId == execRole.RoleId);
        }

        var pendingReqs = await _uow.AssetRequests.CountAsync(r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview || r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking);
        var approvedReqs = await _uow.AssetRequests.CountAsync(r => r.Status == RequestStatus.Allocated);
        var rejectedReqs = await _uow.AssetRequests.CountAsync(r => r.Status == RequestStatus.Rejected);

        // Assets by Category
        var categories = await _uow.AssetCategories.GetAllAsync();
        var allAssets = await _uow.Assets.FindAsync(a => a.IsActive);
        var colors = new[] { "#2563EB", "#0284C7", "#0D9488", "#16A34A", "#CA8A04", "#EA580C", "#9333EA", "#E11D48", "#64748B" };
        
        int colorIdx = 0;
        var categoryCharts = categories.Select(c => new CategoryChartItemDto
        {
            Name = c.CategoryName,
            Value = allAssets.Count(a => a.AssetCategoryId == c.AssetCategoryId),
            Color = colors[colorIdx++ % colors.Length]
        }).Where(c => c.Value > 0).ToList();

        var statusCharts = new List<StatusChartItemDto>
        {
            new() { Name = "Assigned", Value = assigned, Color = "#2563EB" },
            new() { Name = "Available", Value = available, Color = "#16A34A" },
            new() { Name = "Maintenance", Value = underRepair, Color = "#EA580C" },
            new() { Name = "Damaged", Value = damaged, Color = "#DC2626" }
        };

        // Recent pending requests
        var recentPending = (await _uow.AssetRequests.FindAsync(r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview))
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToList();

        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var catDict = categories.ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);

        var pendingDtos = recentPending.Select(r => new AssetRequestDto
        {
            RequestId = r.RequestId,
            RequestNumber = r.RequestNumber,
            EmployeeId = r.EmployeeId,
            EmployeeCode = employees.TryGetValue(r.EmployeeId, out var emp) ? emp.EmployeeCode : "",
            EmployeeName = emp?.FullName ?? "Unknown",
            DepartmentName = emp?.DepartmentId.HasValue == true ? "IT/Operations" : "",
            AssetCategoryId = r.AssetCategoryId,
            CategoryName = catDict.TryGetValue(r.AssetCategoryId, out var cname) ? cname : "",
            Reason = r.Reason,
            RequiredDate = r.RequiredDate,
            Priority = r.Priority,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Result<ManagerDashboardDto>.Success(new ManagerDashboardDto
        {
            TotalAssets = totalAssets,
            AssignedAssets = assigned,
            AvailableAssets = available,
            UnderRepairAssets = underRepair,
            DamagedAssets = damaged,
            ExecutiveCount = execCount,
            PendingRequestsCount = pendingReqs,
            ApprovedRequestsCount = approvedReqs,
            RejectedRequestsCount = rejectedReqs,
            AssetsByCategory = categoryCharts,
            AssetsByStatus = statusCharts,
            PendingRequests = pendingDtos
        });
    }

    public async Task<Result<ExecutiveDashboardDto>> GetExecutiveDashboardAsync()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<ExecutiveDashboardDto>.Failure("User not authenticated.", "UNAUTHORIZED");
        }

        var assignedTasks = (await _uow.AssetRequests.FindAsync(r => r.AssignedExecutiveId == userId.Value && 
            (r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking || r.Status == RequestStatus.NotAvailable)))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);

        var taskDtos = assignedTasks.Select(r => new AssetRequestDto
        {
            RequestId = r.RequestId,
            RequestNumber = r.RequestNumber,
            EmployeeId = r.EmployeeId,
            EmployeeCode = employees.TryGetValue(r.EmployeeId, out var emp) ? emp.EmployeeCode : "",
            EmployeeName = emp?.FullName ?? "Unknown",
            AssetCategoryId = r.AssetCategoryId,
            CategoryName = categories.TryGetValue(r.AssetCategoryId, out var cname) ? cname : "",
            Reason = r.Reason,
            RequiredDate = r.RequiredDate,
            Priority = r.Priority,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        var assignedRepairs = (await _uow.AssetRepairs.FindAsync(r => r.HandledByExecutiveId == userId.Value &&
            (r.Status == RepairStatus.Reported || r.Status == RepairStatus.UnderReview || r.Status == RepairStatus.InRepair)))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);

        var repairDtos = assignedRepairs.Select(r => new AssetRepairDto
        {
            RepairId = r.RepairId,
            AssetId = r.AssetId,
            AssetCode = assets.TryGetValue(r.AssetId, out var ast) ? ast.AssetCode : "",
            AssetName = ast?.AssetName ?? "",
            ReportedByEmployeeId = r.ReportedByEmployeeId,
            ReportedByEmployeeName = employees.TryGetValue(r.ReportedByEmployeeId, out var remp) ? remp.FullName : "",
            HandledByExecutiveId = r.HandledByExecutiveId,
            IssueDescription = r.IssueDescription,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        int availableToAssign = await _uow.Assets.CountAsync(a => a.IsActive && a.Status == AssetStatus.Available);

        return Result<ExecutiveDashboardDto>.Success(new ExecutiveDashboardDto
        {
            MyPendingTasksCount = taskDtos.Count,
            AssetRequestsCount = taskDtos.Count,
            AssetsToAssignCount = availableToAssign,
            RepairRequestsCount = repairDtos.Count,
            MyAssignedTasks = taskDtos,
            MyAssignedRepairs = repairDtos
        });
    }
}
