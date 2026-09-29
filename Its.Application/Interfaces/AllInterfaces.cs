using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Strategies;
using Its.Domain.Entities;
using Its.Domain.Enums;
using System.Linq.Expressions;

namespace Its.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    IQueryable<T> Query();
}

public interface IUnitOfWork : IDisposable
{
    IRepository<Department> Departments { get; }
    IRepository<Employee> Employees { get; }
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<RolePermission> RolePermissions { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<AssetCategory> AssetCategories { get; }
    IRepository<ITAsset> Assets { get; }
    IRepository<AssetRequest> AssetRequests { get; }
    IRepository<AssetRequestHistory> RequestHistories { get; }
    IRepository<AssetAllocation> AssetAllocations { get; }
    IRepository<AssetRepair> AssetRepairs { get; }
    IRepository<AssetReplacement> AssetReplacements { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
    Func<Task> operation,
    CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    long? UserId { get; }
    long? EmployeeId { get; }
    string? Username { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    string? IpAddress { get; }
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface INotificationPublisher
{
    Task PublishNotificationAsync(long userId, string title, string message, string type, string? relatedEntity = null, long? relatedId = null);
    Task PublishNotificationToRoleAsync(string roleName, string title, string message, string type, string? relatedEntity = null, long? relatedId = null);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, object? oldValue = null, object? newValue = null);
}

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result> LogoutAsync();
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    Task<Result<UserDto>> GetCurrentUserAsync();
}

public interface IDashboardService
{
    Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync();
    Task<Result<ManagerDashboardDto>> GetManagerDashboardAsync();
    Task<Result<ExecutiveDashboardDto>> GetExecutiveDashboardAsync();
}

public interface IEmployeeService
{
    Task<Result<PagedResult<EmployeeDto>>> GetEmployeesAsync(QueryParameters parameters);
    Task<Result<EmployeeDto>> GetEmployeeByIdAsync(long id);
    Task<Result<List<AssetDto>>> GetEmployeeAssetsAsync(long employeeId);
    Task<Result<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<Result<EmployeeDto>> UpdateEmployeeAsync(long id, UpdateEmployeeDto dto);
}

public interface IExecutiveService
{
    Task<Result<PagedResult<ExecutiveDto>>> GetExecutivesAsync(QueryParameters parameters);
    Task<Result<ExecutiveDto>> GetExecutiveByIdAsync(long id);
    Task<Result<ExecutiveDto>> CreateExecutiveAsync(CreateExecutiveDto dto);
    Task<Result<ExecutiveDto>> UpdateExecutiveAsync(long id, UpdateExecutiveDto dto);
    Task<Result> ChangeExecutiveStatusAsync(long id, ChangeStatusDto dto);
}

public interface IAssetService
{
    Task<Result<PagedResult<AssetDto>>> GetAssetsAsync(AssetFilterDto filter);
    Task<Result<AssetDto>> GetAssetByIdAsync(long id);
    Task<Result<AssetDto>> CreateAssetAsync(CreateAssetDto dto);
    Task<Result<AssetDto>> UpdateAssetAsync(long id, UpdateAssetDto dto);
    Task<Result> DeactivateAssetAsync(long id);
    Task<Result<List<AssetDto>>> GetAvailableAssetsAsync(int? categoryId = null);
    Task<Result<AssetCheckResult>> CheckAssetAsync(long assetId, long? employeeId = null);
    Task<Result<List<AssetCategoryDto>>> GetCategoriesAsync();
    Task<Result<AssetCategoryDto>> CreateCategoryAsync(AssetCategoryDto dto);
    Task<Result<AssetCategoryDto>> UpdateCategoryAsync(int id, AssetCategoryDto dto);
}

public interface IAllocationService
{
    Task<Result<AssetDto>> AssignAssetAsync(long assetId, AssignAssetDto dto);
    Task<Result<AssetDto>> ReturnAssetAsync(long assetId, ReturnAssetDto dto);
    Task<Result<AssetDto>> TransferAssetAsync(long assetId, TransferAssetDto dto);
    Task<Result<List<AllocationHistoryDto>>> GetAllocationHistoryAsync(long assetId);
    Task<Result<PagedResult<AllocationHistoryDto>>> GetAllAllocationsAsync(QueryParameters parameters);
}

public interface IRequestService
{
    Task<Result<PagedResult<AssetRequestDto>>> GetAllRequestsAsync(QueryParameters parameters, RequestStatus? status = null);
    Task<Result<PagedResult<AssetRequestDto>>> GetMyRequestsAsync(QueryParameters parameters);
    Task<Result<PagedResult<AssetRequestDto>>> GetAssignedRequestsAsync(QueryParameters parameters);
    Task<Result<AssetRequestDto>> GetRequestByIdAsync(long id);
    Task<Result<AssetRequestDto>> CreateRequestAsync(CreateAssetRequestDto dto);
    Task<Result<AssetRequestDto>> UpdateRequestAsync(long id, UpdateAssetRequestDto dto);
    Task<Result<AssetRequestDto>> ValidateRequestAsync(long id, ValidateRequestDto dto);
    Task<Result<AssetRequestDto>> AssignExecutiveAsync(long id, AssignExecutiveDto dto);
    Task<Result<AssetRequestDto>> RejectRequestAsync(long id, RejectRequestDto dto);
    Task<Result<AssetRequestDto>> AllocateRequestAsync(long id, AllocateRequestDto dto);
}

public interface IRepairService
{
    Task<Result<PagedResult<AssetRepairDto>>> GetRepairsAsync(QueryParameters parameters, RepairStatus? status = null);
    Task<Result<AssetRepairDto>> GetRepairByIdAsync(long id);
    Task<Result<AssetRepairDto>> CreateRepairAsync(CreateRepairDto dto);
    Task<Result<AssetRepairDto>> UpdateRepairStatusAsync(long id, UpdateRepairStatusDto dto);
}

public interface IReplacementService
{
    Task<Result<PagedResult<AssetReplacementDto>>> GetReplacementsAsync(QueryParameters parameters, ReplacementStatus? status = null);
    Task<Result<AssetReplacementDto>> GetReplacementByIdAsync(long id);
    Task<Result<AssetReplacementDto>> CreateReplacementAsync(CreateReplacementDto dto);
    Task<Result<AssetReplacementDto>> ProcessReplacementAsync(long id, ReplacementDecisionDto dto);
}

public interface INotificationService
{
    Task<Result<List<NotificationDto>>> GetNotificationsAsync();
    Task<Result> MarkAsReadAsync(long id);
    Task<Result> MarkAllAsReadAsync();
}

public interface IReportService
{
    Task<byte[]> GenerateAssetReportCsvAsync();
    Task<byte[]> GenerateAllocationReportCsvAsync();
    Task<byte[]> GenerateRepairReportCsvAsync();
    Task<object> GetAssetReportDataAsync();
    Task<object> GetAllocationReportDataAsync();
    Task<object> GetRepairReportDataAsync();
}
