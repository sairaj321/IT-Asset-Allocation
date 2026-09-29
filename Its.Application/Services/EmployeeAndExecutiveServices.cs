using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Interfaces;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Domain.Exceptions;

namespace Its.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public EmployeeService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<Result<PagedResult<EmployeeDto>>> GetEmployeesAsync(QueryParameters parameters)
    {
        var allEmployees = await _uow.Employees.GetAllAsync();
        var departments = (await _uow.Departments.GetAllAsync()).ToDictionary(d => d.DepartmentId, d => d.Name);
        var allocations = await _uow.AssetAllocations.FindAsync(a => a.AllocationStatus == AllocationStatus.Active);
        var pendingRequests = await _uow.AssetRequests.FindAsync(r => r.Status != RequestStatus.Allocated && r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Cancelled);

        var query = allEmployees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(e => e.EmployeeCode.ToLower().Contains(search) ||
                                     e.FirstName.ToLower().Contains(search) ||
                                     e.LastName.ToLower().Contains(search) ||
                                     e.Email.ToLower().Contains(search) ||
                                     (e.Designation != null && e.Designation.ToLower().Contains(search)));
        }

        var list = query.ToList();
        var totalCount = list.Count;

        var pagedItems = list
            .OrderBy(e => e.FirstName)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Mobile = e.Mobile,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.DepartmentId.HasValue && departments.TryGetValue(e.DepartmentId.Value, out var dname) ? dname : null,
                Designation = e.Designation,
                IsActive = e.IsActive,
                AllocatedAssetsCount = allocations.Count(a => a.EmployeeId == e.EmployeeId),
                PendingRequestsCount = pendingRequests.Count(r => r.EmployeeId == e.EmployeeId)
            }).ToList();

        return Result<PagedResult<EmployeeDto>>.Success(new PagedResult<EmployeeDto>(pagedItems, totalCount, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<EmployeeDto>> GetEmployeeByIdAsync(long id)
    {
        var employee = await _uow.Employees.GetByIdAsync(id);
        if (employee == null)
        {
            return Result<EmployeeDto>.Failure("Employee not found.", "NOT_FOUND");
        }

        string? deptName = null;
        if (employee.DepartmentId.HasValue)
        {
            var dept = await _uow.Departments.GetByIdAsync(employee.DepartmentId.Value);
            deptName = dept?.Name;
        }

        var allocCount = await _uow.AssetAllocations.CountAsync(a => a.EmployeeId == id && a.AllocationStatus == AllocationStatus.Active);
        var pendingCount = await _uow.AssetRequests.CountAsync(r => r.EmployeeId == id && (r.Status == RequestStatus.Submitted || r.Status == RequestStatus.UnderReview || r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking));

        return Result<EmployeeDto>.Success(new EmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Mobile = employee.Mobile,
            DepartmentId = employee.DepartmentId,
            DepartmentName = deptName,
            Designation = employee.Designation,
            IsActive = employee.IsActive,
            AllocatedAssetsCount = allocCount,
            PendingRequestsCount = pendingCount
        });
    }

    public async Task<Result<List<AssetDto>>> GetEmployeeAssetsAsync(long employeeId)
    {
        var allocations = await _uow.AssetAllocations.FindAsync(a => a.EmployeeId == employeeId && a.AllocationStatus == AllocationStatus.Active);
        var assetIds = allocations.Select(a => a.AssetId).ToList();
        var assets = await _uow.Assets.FindAsync(a => assetIds.Contains(a.AssetId));
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var employee = await _uow.Employees.GetByIdAsync(employeeId);

        var result = assets.Select(a =>
        {
            var alloc = allocations.FirstOrDefault(x => x.AssetId == a.AssetId);
            return new AssetDto
            {
                AssetId = a.AssetId,
                AssetCode = a.AssetCode,
                AssetName = a.AssetName,
                AssetCategoryId = a.AssetCategoryId,
                CategoryName = categories.TryGetValue(a.AssetCategoryId, out var cname) ? cname : "",
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
                CurrentEmployeeId = employeeId,
                AssignedEmployeeCode = employee?.EmployeeCode,
                AssignedEmployeeName = employee?.FullName,
                AllocationDate = alloc?.AllocationDate,
                ExpectedReturnDate = alloc?.ExpectedReturnDate,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt
            };
        }).ToList();

        return Result<List<AssetDto>>.Success(result);
    }

    public async Task<Result<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var existing = await _uow.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == dto.EmployeeCode.Trim().ToLower() || e.Email.ToLower() == dto.Email.Trim().ToLower());
        if (existing != null)
        {
            return Result<EmployeeDto>.Failure("An employee with the same employee code or email already exists.", "DUPLICATE");
        }

        var employee = new Employee
        {
            EmployeeCode = dto.EmployeeCode.Trim().ToUpper(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Mobile = dto.Mobile?.Trim(),
            DepartmentId = dto.DepartmentId,
            Designation = dto.Designation?.Trim(),
            IsActive = true
        };

        await _uow.Employees.AddAsync(employee);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Create Employee", "Employee", employee.EmployeeId.ToString(), null, employee);

        return await GetEmployeeByIdAsync(employee.EmployeeId);
    }

    public async Task<Result<EmployeeDto>> UpdateEmployeeAsync(long id, UpdateEmployeeDto dto)
    {
        var employee = await _uow.Employees.GetByIdAsync(id);
        if (employee == null)
        {
            return Result<EmployeeDto>.Failure("Employee not found.", "NOT_FOUND");
        }

        var oldVal = new { employee.FirstName, employee.LastName, employee.Email, employee.Designation, employee.IsActive };

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Email = dto.Email.Trim().ToLower();
        employee.Mobile = dto.Mobile?.Trim();
        employee.DepartmentId = dto.DepartmentId;
        employee.Designation = dto.Designation?.Trim();
        employee.IsActive = dto.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        _uow.Employees.Update(employee);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Update Employee", "Employee", employee.EmployeeId.ToString(), oldVal, employee);

        return await GetEmployeeByIdAsync(id);
    }
}

public class ExecutiveService : IExecutiveService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public ExecutiveService(IUnitOfWork uow, IPasswordHasher passwordHasher, IAuditService auditService)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<Result<PagedResult<ExecutiveDto>>> GetExecutivesAsync(QueryParameters parameters)
    {
        var execRole = await _uow.Roles.FirstOrDefaultAsync(r => r.Name == "ITS_EXECUTIVE");
        if (execRole == null)
        {
            return Result<PagedResult<ExecutiveDto>>.Success(new PagedResult<ExecutiveDto>(new List<ExecutiveDto>(), 0, 1, 10));
        }

        var userRoles = await _uow.UserRoles.FindAsync(ur => ur.RoleId == execRole.RoleId);
        var userIds = userRoles.Select(ur => ur.UserId).ToList();
        var users = await _uow.Users.FindAsync(u => userIds.Contains(u.UserId));
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var departments = (await _uow.Departments.GetAllAsync()).ToDictionary(d => d.DepartmentId, d => d.Name);

        var allTasks = await _uow.AssetRequests.GetAllAsync();

        var list = users.Select(u =>
        {
            employees.TryGetValue(u.EmployeeId, out var emp);
            string? deptName = null;
            if (emp?.DepartmentId.HasValue == true && departments.TryGetValue(emp.DepartmentId.Value, out var dname))
            {
                deptName = dname;
            }

            var openTasks = allTasks.Count(r => r.AssignedExecutiveId == u.UserId && (r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking));
            var completedTasks = allTasks.Count(r => r.AssignedExecutiveId == u.UserId && (r.Status == RequestStatus.Allocated || r.Status == RequestStatus.Rejected));

            return new ExecutiveDto
            {
                UserId = u.UserId,
                EmployeeId = u.EmployeeId,
                EmployeeCode = emp?.EmployeeCode ?? "",
                FirstName = emp?.FirstName ?? "",
                LastName = emp?.LastName ?? "",
                Email = emp?.Email ?? "",
                Mobile = emp?.Mobile,
                DepartmentName = deptName,
                Username = u.Username,
                IsActive = u.IsActive,
                OpenTasksCount = openTasks,
                CompletedTasksCount = completedTasks,
                CreatedAt = u.CreatedAt
            };
        }).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            list = list.Where(e => e.FirstName.ToLower().Contains(search) ||
                                   e.LastName.ToLower().Contains(search) ||
                                   e.Username.ToLower().Contains(search) ||
                                   e.EmployeeCode.ToLower().Contains(search) ||
                                   e.Email.ToLower().Contains(search));
        }

        var materialized = list.ToList();
        var total = materialized.Count;
        var items = materialized
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        return Result<PagedResult<ExecutiveDto>>.Success(new PagedResult<ExecutiveDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<ExecutiveDto>> GetExecutiveByIdAsync(long id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
        {
            return Result<ExecutiveDto>.Failure("Executive user not found.", "NOT_FOUND");
        }

        var emp = await _uow.Employees.GetByIdAsync(user.EmployeeId);
        string? deptName = null;
        if (emp?.DepartmentId.HasValue == true)
        {
            var dept = await _uow.Departments.GetByIdAsync(emp.DepartmentId.Value);
            deptName = dept?.Name;
        }

        var openTasks = await _uow.AssetRequests.CountAsync(r => r.AssignedExecutiveId == user.UserId && (r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking));
        var completedTasks = await _uow.AssetRequests.CountAsync(r => r.AssignedExecutiveId == user.UserId && (r.Status == RequestStatus.Allocated || r.Status == RequestStatus.Rejected));

        return Result<ExecutiveDto>.Success(new ExecutiveDto
        {
            UserId = user.UserId,
            EmployeeId = user.EmployeeId,
            EmployeeCode = emp?.EmployeeCode ?? "",
            FirstName = emp?.FirstName ?? "",
            LastName = emp?.LastName ?? "",
            Email = emp?.Email ?? "",
            Mobile = emp?.Mobile,
            DepartmentName = deptName,
            Username = user.Username,
            IsActive = user.IsActive,
            OpenTasksCount = openTasks,
            CompletedTasksCount = completedTasks,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<Result<ExecutiveDto>> CreateExecutiveAsync(CreateExecutiveDto dto)
    {
        // Check if username exists
        var existingUser = await _uow.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.Trim().ToLower());
        if (existingUser != null)
        {
            return Result<ExecutiveDto>.Failure("Username is already taken.", "DUPLICATE_USERNAME");
        }

        var execRole = await _uow.Roles.FirstOrDefaultAsync(r => r.Name == "ITS_EXECUTIVE");
        if (execRole == null)
        {
            return Result<ExecutiveDto>.Failure("ITS_EXECUTIVE role not configured.", "ROLE_MISSING");
        }

        // Check if employee already exists by EmployeeCode or Email
        var employee = await _uow.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == dto.EmployeeCode.Trim().ToLower() || e.Email.ToLower() == dto.Email.Trim().ToLower());

        if (employee == null)
        {
            // Create employee
            employee = new Employee
            {
                EmployeeCode = dto.EmployeeCode.Trim().ToUpper(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Mobile = dto.Mobile?.Trim(),
                DepartmentId = dto.DepartmentId,
                Designation = "ITS Executive",
                IsActive = dto.IsActive
            };
            await _uow.Employees.AddAsync(employee);
            await _uow.SaveChangesAsync();
        }

        // Check if employee already has a user account
        var existingUserForEmp = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);
        if (existingUserForEmp != null)
        {
            return Result<ExecutiveDto>.Failure($"Employee '{employee.FullName}' already has a user account.", "USER_EXISTS");
        }

        // Create User with temporary password
        var newUser = new User
        {
            EmployeeId = employee.EmployeeId,
            Username = dto.Username.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.TemporaryPassword),
            MustChangePassword = true,
            IsActive = dto.IsActive
        };

        await _uow.Users.AddAsync(newUser);
        await _uow.SaveChangesAsync();

        // Assign ITS_EXECUTIVE role
        await _uow.UserRoles.AddAsync(new UserRole
        {
            UserId = newUser.UserId,
            RoleId = execRole.RoleId
        });

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Create ITS Executive", "User", newUser.UserId.ToString(), null, new { newUser.Username, EmployeeCode = employee.EmployeeCode });

        return await GetExecutiveByIdAsync(newUser.UserId);
    }

    public async Task<Result<ExecutiveDto>> UpdateExecutiveAsync(long id, UpdateExecutiveDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
        {
            return Result<ExecutiveDto>.Failure("Executive user not found.", "NOT_FOUND");
        }

        var emp = await _uow.Employees.GetByIdAsync(user.EmployeeId);
        if (emp != null)
        {
            emp.FirstName = dto.FirstName.Trim();
            emp.LastName = dto.LastName.Trim();
            emp.Email = dto.Email.Trim().ToLower();
            emp.Mobile = dto.Mobile?.Trim();
            emp.DepartmentId = dto.DepartmentId;
            emp.IsActive = dto.IsActive;
            _uow.Employees.Update(emp);
        }

        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Update ITS Executive", "User", user.UserId.ToString(), null, dto);

        return await GetExecutiveByIdAsync(id);
    }

    public async Task<Result> ChangeExecutiveStatusAsync(long id, ChangeStatusDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
        {
            return Result.Failure("Executive user not found.", "NOT_FOUND");
        }

        if (!dto.IsActive)
        {
            // Check for open tasks
            var openTasks = await _uow.AssetRequests.FindAsync(r => r.AssignedExecutiveId == id && (r.Status == RequestStatus.AssignedToExecutive || r.Status == RequestStatus.Checking));
            if (openTasks.Any())
            {
                if (!dto.ReassignTasksToExecutiveId.HasValue)
                {
                    return Result.Failure($"Cannot deactivate executive because they have {openTasks.Count} open task(s). Please specify a replacement executive to reassign tasks to.", "OPEN_TASKS_EXIST");
                }

                var targetExec = await _uow.Users.GetByIdAsync(dto.ReassignTasksToExecutiveId.Value);
                if (targetExec == null || !targetExec.IsActive)
                {
                    return Result.Failure("Selected reassignment executive is not valid or active.", "INVALID_TARGET_EXEC");
                }

                foreach (var task in openTasks)
                {
                    task.AssignedExecutiveId = dto.ReassignTasksToExecutiveId.Value;
                    task.UpdatedAt = DateTime.UtcNow;
                    _uow.AssetRequests.Update(task);

                    await _uow.RequestHistories.AddAsync(new AssetRequestHistory
                    {
                        RequestId = task.RequestId,
                        FromStatus = task.Status,
                        ToStatus = task.Status,
                        Remarks = $"Reassigned task from deactivated executive to {targetExec.Username}."
                    });
                }
            }
        }

        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Change Executive Status", "User", user.UserId.ToString(), null, new { dto.IsActive, dto.ReassignTasksToExecutiveId });

        return Result.Success($"Executive status updated to {(dto.IsActive ? "Active" : "Inactive")}.");
    }
}
