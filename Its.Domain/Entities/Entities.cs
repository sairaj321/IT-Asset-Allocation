using Its.Domain.Common;
using Its.Domain.Enums;

namespace Its.Domain.Entities;

public class Department : BaseEntity
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigations
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee : BaseEntity
{
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigations
    public Department? Department { get; set; }
    public User? User { get; set; }
    public ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();
    public ICollection<AssetAllocation> AssetAllocations { get; set; } = new List<AssetAllocation>();
    public ICollection<AssetRepair> AssetRepairs { get; set; } = new List<AssetRepair>();
    public ICollection<AssetReplacement> AssetReplacements { get; set; } = new List<AssetReplacement>();
}

public class User : BaseEntity
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    // Navigations
    public Employee Employee { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AssetRequest> AssignedRequests { get; set; } = new List<AssetRequest>();
    public ICollection<AssetRepair> HandledRepairs { get; set; } = new List<AssetRepair>();
}

public class Role : BaseEntity
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigations
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : BaseEntity
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigations
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class UserRole
{
    public long UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public class RefreshToken : BaseEntity
{
    public long TokenId { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    public User User { get; set; } = null!;
}

public class AssetCategory : BaseEntity
{
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigations
    public ICollection<ITAsset> Assets { get; set; } = new List<ITAsset>();
    public ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();
}

public class ITAsset : BaseEntity
{
    public long AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int AssetCategoryId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Location { get; set; }
    public long? CurrentEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; } // For optimistic concurrency (xmin / concurrency token)

    // Navigations
    public AssetCategory Category { get; set; } = null!;
    public Employee? CurrentEmployee { get; set; }
    public ICollection<AssetAllocation> AssetAllocations { get; set; } = new List<AssetAllocation>();
    public ICollection<AssetRepair> AssetRepairs { get; set; } = new List<AssetRepair>();
    public ICollection<AssetReplacement> ReplacementsAsOld { get; set; } = new List<AssetReplacement>();
    public ICollection<AssetReplacement> ReplacementsAsNew { get; set; } = new List<AssetReplacement>();
}

public class AssetRequest : BaseEntity
{
    public long RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public int AssetCategoryId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? RequiredDate { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public RequestStatus Status { get; set; } = RequestStatus.Submitted;
    public long? AssignedExecutiveId { get; set; }
    public string? ManagerRemarks { get; set; }
    public string? RejectionReason { get; set; }
    public long? AllocatedAssetId { get; set; }
    public RequestType RequestType { get; set; } = RequestType.NewAsset;

    // Navigations
    public Employee Employee { get; set; } = null!;
    public AssetCategory AssetCategory { get; set; } = null!;
    public User? AssignedExecutive { get; set; }
    public ITAsset? AllocatedAsset { get; set; }
    public ICollection<AssetRequestHistory> RequestHistories { get; set; } = new List<AssetRequestHistory>();
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}

public class AssetRequestHistory : BaseEntity
{
    public long HistoryId { get; set; }
    public long RequestId { get; set; }
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public long? ActionBy { get; set; }
    public string? Remarks { get; set; }

    // Navigations
    public AssetRequest Request { get; set; } = null!;
    public User? ActionByUser { get; set; }
}

public class AssetAllocation : BaseEntity
{
    public long AllocationId { get; set; }
    public long AssetId { get; set; }
    public long EmployeeId { get; set; }
    public long? RequestId { get; set; }
    public long AllocatedBy { get; set; }
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public AllocationStatus AllocationStatus { get; set; } = AllocationStatus.Active;
    public AssetCondition? ConditionAtAllocation { get; set; }
    public AssetCondition? ConditionAtReturn { get; set; }
    public string? Remarks { get; set; }

    // Navigations
    public ITAsset Asset { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public AssetRequest? Request { get; set; }
    public User AllocatedByUser { get; set; } = null!;
}

public class AssetRepair : BaseEntity
{
    public long RepairId { get; set; }
    public long AssetId { get; set; }
    public long ReportedByEmployeeId { get; set; }
    public long? HandledByExecutiveId { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public RepairStatus Status { get; set; } = RepairStatus.Reported;
    public string? VendorName { get; set; }
    public decimal? RepairCost { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public AssetCondition? ConditionAfterRepair { get; set; }

    // Navigations
    public ITAsset Asset { get; set; } = null!;
    public Employee ReportedByEmployee { get; set; } = null!;
    public User? HandledByExecutive { get; set; }
}

public class AssetReplacement : BaseEntity
{
    public long ReplacementId { get; set; }
    public long OldAssetId { get; set; }
    public long? NewAssetId { get; set; }
    public long EmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReplacementStatus Status { get; set; } = ReplacementStatus.Requested;
    public long? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Navigations
    public ITAsset OldAsset { get; set; } = null!;
    public ITAsset? NewAsset { get; set; }
    public Employee Employee { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
}

public class Notification : BaseEntity
{
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Success, Warning, Error
    public string? RelatedEntity { get; set; } // AssetRequest, ITAsset, AssetRepair, etc.
    public long? RelatedId { get; set; }
    public bool IsRead { get; set; } = false;

    // Navigations
    public User User { get; set; } = null!;
}

public class AuditLog : BaseEntity
{
    public long AuditId { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }

    // Navigations
    public User? User { get; set; }
}
