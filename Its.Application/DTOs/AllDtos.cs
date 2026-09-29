using Its.Application.Strategies;
using Its.Domain.Enums;

namespace Its.Application.DTOs;

#region Auth DTOs
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UserDto
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? DepartmentName { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
#endregion

#region Department & Employee DTOs
public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmployeeDto
{
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Designation { get; set; }
    public bool IsActive { get; set; }
    public int AllocatedAssetsCount { get; set; }
    public int PendingRequestsCount { get; set; }
}

public class CreateEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public string? Designation { get; set; }
}

public class UpdateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;
}
#endregion

#region Executive DTOs
public class ExecutiveDto
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? DepartmentName { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OpenTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateExecutiveDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateExecutiveDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}

public class ChangeStatusDto
{
    public bool IsActive { get; set; }
    public long? ReassignTasksToExecutiveId { get; set; }
}
#endregion

#region Category & Asset DTOs
public class AssetCategoryDto
{
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int TotalAssetsCount { get; set; }
    public int AvailableAssetsCount { get; set; }
}

public class AssetDto
{
    public long AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public AssetStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public AssetCondition Condition { get; set; }
    public string ConditionName => Condition.ToString();
    public string? Location { get; set; }
    public long? CurrentEmployeeId { get; set; }
    public string? AssignedEmployeeCode { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public string? AssignedEmployeeDepartment { get; set; }
    public DateTime? AllocationDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public bool IsActive { get; set; }
    public uint Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssetDto
{
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
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Location { get; set; }
}

public class UpdateAssetDto
{
    public string AssetName { get; set; } = string.Empty;
    public int AssetCategoryId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public AssetCondition Condition { get; set; }
    public AssetStatus Status { get; set; }
    public string? Location { get; set; }
}

public class AssetFilterDto : Common.QueryParameters
{
    public int? CategoryId { get; set; }
    public AssetStatus? Status { get; set; }
    public AssetCondition? Condition { get; set; }
    public bool? ExpiringWarranty { get; set; }
}
#endregion

#region Allocation DTOs
public class AssignAssetDto
{
    public long EmployeeId { get; set; }
    public long? RequestId { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Remarks { get; set; }
}

public class ReturnAssetDto
{
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public AssetCondition ConditionAtReturn { get; set; } = AssetCondition.Good;
    public string? Remarks { get; set; }
    public AssetStatus? ResultingStatus { get; set; } // Available, Damaged, UnderMaintenance
}

public class TransferAssetDto
{
    public long TargetEmployeeId { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Remarks { get; set; }
}

public class AllocationHistoryDto
{
    public long AllocationId { get; set; }
    public long AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public DateTime AllocationDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public AllocationStatus AllocationStatus { get; set; }
    public string StatusName => AllocationStatus.ToString();
    public AssetCondition? ConditionAtAllocation { get; set; }
    public AssetCondition? ConditionAtReturn { get; set; }
    public string? Remarks { get; set; }
    public string AllocatedByName { get; set; } = string.Empty;
}
#endregion

#region Request DTOs
public class AssetRequestDto
{
    public long RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int AssetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? RequiredDate { get; set; }
    public Priority Priority { get; set; }
    public string PriorityName => Priority.ToString();
    public RequestStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public long? AssignedExecutiveId { get; set; }
    public string? AssignedExecutiveName { get; set; }
    public string? ManagerRemarks { get; set; }
    public string? RejectionReason { get; set; }
    public long? AllocatedAssetId { get; set; }
    public string? AllocatedAssetCode { get; set; }
    public string? AllocatedAssetName { get; set; }
    public RequestType RequestType { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RequestHistoryDto> History { get; set; } = new();
}

public class RequestHistoryDto
{
    public long HistoryId { get; set; }
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public string ActionByName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssetRequestDto
{
    public int AssetCategoryId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? RequiredDate { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public RequestType RequestType { get; set; } = RequestType.NewAsset;
}

public class UpdateAssetRequestDto
{
    public int AssetCategoryId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? RequiredDate { get; set; }
    public Priority Priority { get; set; }
}

public class ValidateRequestDto
{
    public bool IsValid { get; set; }
    public long? ExecutiveId { get; set; }
    public string? Remarks { get; set; }
    public string? RejectionReason { get; set; }
    public bool ReturnForCorrection { get; set; }
}

public class AssignExecutiveDto
{
    public long ExecutiveId { get; set; }
    public string? Remarks { get; set; }
}

public class RejectRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class AllocateRequestDto
{
    public long AssetId { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Remarks { get; set; }
}
#endregion

#region Repair DTOs
public class AssetRepairDto
{
    public long RepairId { get; set; }
    public long AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public long ReportedByEmployeeId { get; set; }
    public string ReportedByEmployeeName { get; set; } = string.Empty;
    public long? HandledByExecutiveId { get; set; }
    public string? HandledByExecutiveName { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public RepairStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? VendorName { get; set; }
    public decimal? RepairCost { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public AssetCondition? ConditionAfterRepair { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRepairDto
{
    public long AssetId { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
}

public class UpdateRepairStatusDto
{
    public RepairStatus Status { get; set; }
    public string? VendorName { get; set; }
    public decimal? RepairCost { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public AssetCondition? ConditionAfterRepair { get; set; }
    public AssetStatus? ResultingAssetStatus { get; set; }
    public string? Remarks { get; set; }
}
#endregion

#region Replacement DTOs
public class AssetReplacementDto
{
    public long ReplacementId { get; set; }
    public long OldAssetId { get; set; }
    public string OldAssetCode { get; set; } = string.Empty;
    public string OldAssetName { get; set; } = string.Empty;
    public long? NewAssetId { get; set; }
    public string? NewAssetCode { get; set; }
    public string? NewAssetName { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ReplacementStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReplacementDto
{
    public long OldAssetId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReplacementDecisionDto
{
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
    public long? NewAssetId { get; set; }
}
#endregion

#region Dashboard DTOs
public class EmployeeDashboardDto
{
    public List<AssetRequestDto> MyRequests { get; set; } = new();
    public List<AssetDto> MyAssets { get; set; } = new();
    public int ActiveAssetsCount { get; set; }
    public int PendingRequestsCount { get; set; }
}

public class CategoryChartItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = "#2563EB";
}

public class StatusChartItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = "#16A34A";
}

public class ManagerDashboardDto
{
    public int TotalAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int AvailableAssets { get; set; }
    public int UnderRepairAssets { get; set; }
    public int DamagedAssets { get; set; }
    public int ExecutiveCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int ApprovedRequestsCount { get; set; }
    public int RejectedRequestsCount { get; set; }
    public List<CategoryChartItemDto> AssetsByCategory { get; set; } = new();
    public List<StatusChartItemDto> AssetsByStatus { get; set; } = new();
    public List<AssetRequestDto> PendingRequests { get; set; } = new();
}

public class ExecutiveDashboardDto
{
    public int MyPendingTasksCount { get; set; }
    public int AssetRequestsCount { get; set; }
    public int AssetsToAssignCount { get; set; }
    public int RepairRequestsCount { get; set; }
    public List<AssetRequestDto> MyAssignedTasks { get; set; } = new();
    public List<AssetRepairDto> MyAssignedRepairs { get; set; } = new();
}
#endregion

#region Notification & Audit DTOs
public class NotificationDto
{
    public long NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string? RelatedEntity { get; set; }
    public long? RelatedId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogDto
{
    public long AuditId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
#endregion
