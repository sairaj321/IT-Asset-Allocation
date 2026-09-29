using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Interfaces;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Domain.State;
using System.Text;
using Serilog;

namespace Its.Application.Services;

public class RequestService : IRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAllocationService _allocationService;
    private readonly IAuditService _auditService;
    private readonly INotificationPublisher _notificationPublisher;

    public RequestService(
        IUnitOfWork uow,
        ICurrentUserService currentUserService,
        IAllocationService allocationService,
        IAuditService auditService,
        INotificationPublisher notificationPublisher)
    {
        _uow = uow;
        _currentUserService = currentUserService;
        _allocationService = allocationService;
        _auditService = auditService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<PagedResult<AssetRequestDto>>> GetAllRequestsAsync(QueryParameters parameters, RequestStatus? status = null)
    {
        var allReqs = await _uow.AssetRequests.GetAllAsync();
        var query = allReqs.AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.RequestNumber.ToLower().Contains(search) ||
                                     r.Reason.ToLower().Contains(search) ||
                                     (employees.TryGetValue(r.EmployeeId, out var emp) && (emp.EmployeeCode.ToLower().Contains(search) || emp.FullName.ToLower().Contains(search))));
        }

        var list = query.ToList();
        var total = list.Count;

        var items = list
            .OrderByDescending(r => r.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r => MapToDto(r, employees, categories, users, assets))
            .ToList();

        return Result<PagedResult<AssetRequestDto>>.Success(new PagedResult<AssetRequestDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<PagedResult<AssetRequestDto>>> GetMyRequestsAsync(QueryParameters parameters)
    {
        var empId = _currentUserService.EmployeeId;
        if (!empId.HasValue)
        {
            return Result<PagedResult<AssetRequestDto>>.Failure("No employee profile found.", "NO_EMPLOYEE");
        }

        var myReqs = await _uow.AssetRequests.FindAsync(r => r.EmployeeId == empId.Value);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);

        var query = myReqs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.RequestNumber.ToLower().Contains(search) || r.Reason.ToLower().Contains(search));
        }

        var list = query.ToList();
        var total = list.Count;
        var items = list
            .OrderByDescending(r => r.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r => MapToDto(r, employees, categories, users, assets))
            .ToList();

        return Result<PagedResult<AssetRequestDto>>.Success(new PagedResult<AssetRequestDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<PagedResult<AssetRequestDto>>> GetAssignedRequestsAsync(QueryParameters parameters)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<PagedResult<AssetRequestDto>>.Failure("User not authenticated.", "UNAUTHORIZED");
        }

        var assigned = await _uow.AssetRequests.FindAsync(r => r.AssignedExecutiveId == userId.Value);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);

        var query = assigned.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.RequestNumber.ToLower().Contains(search) ||
                                     r.Reason.ToLower().Contains(search) ||
                                     (employees.TryGetValue(r.EmployeeId, out var emp) && emp.FullName.ToLower().Contains(search)));
        }

        var list = query.ToList();
        var total = list.Count;
        var items = list
            .OrderByDescending(r => r.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r => MapToDto(r, employees, categories, users, assets))
            .ToList();

        return Result<PagedResult<AssetRequestDto>>.Success(new PagedResult<AssetRequestDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<AssetRequestDto>> GetRequestByIdAsync(long id)
    {
        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Asset request not found.", "NOT_FOUND");
        }

        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);

        var dto = MapToDto(request, employees, categories, users, assets);

        var histories = (await _uow.RequestHistories.FindAsync(h => h.RequestId == id))
            .OrderBy(h => h.CreatedAt)
            .ToList();

        dto.History = histories.Select(h => new RequestHistoryDto
        {
            HistoryId = h.HistoryId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ActionByName = h.ActionBy.HasValue && users.TryGetValue(h.ActionBy.Value, out var u) ? u.Username : "System",
            Remarks = h.Remarks,
            CreatedAt = h.CreatedAt
        }).ToList();

        return Result<AssetRequestDto>.Success(dto);
    }

    public async Task<Result<AssetRequestDto>> CreateRequestAsync(CreateAssetRequestDto dto)
    {
        var empId = _currentUserService.EmployeeId;
        if (!empId.HasValue)
        {
            return Result<AssetRequestDto>.Failure("No employee profile found.", "NO_EMPLOYEE");
        }

        var count = await _uow.AssetRequests.CountAsync();
        var requestNumber = $"REQ-{(count + 1):D4}";

        var request = new AssetRequest
        {
            RequestNumber = requestNumber,
            EmployeeId = empId.Value,
            AssetCategoryId = dto.AssetCategoryId,
            Reason = dto.Reason.Trim(),
            RequiredDate = dto.RequiredDate,
            Priority = dto.Priority,
            Status = RequestStatus.Submitted,
            RequestType = dto.RequestType
        };

        await _uow.AssetRequests.AddAsync(request);
        await _uow.SaveChangesAsync();

        var userId = _currentUserService.UserId;
        await _uow.RequestHistories.AddAsync(new AssetRequestHistory
        {
            RequestId = request.RequestId,
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            ActionBy = userId,
            Remarks = "Asset request submitted by employee."
        });
        await _uow.SaveChangesAsync();

        // Notify ITS Managers
        var employee = await _uow.Employees.GetByIdAsync(empId.Value);
        await _notificationPublisher.PublishNotificationToRoleAsync(
            "ITS_MANAGER",
            "New Asset Request",
            $"New request {request.RequestNumber} submitted by {employee?.FullName ?? "Employee"}.",
            "Info",
            "AssetRequest",
            request.RequestId);

        await _auditService.LogAsync("Create Request", "AssetRequest", request.RequestId.ToString(), null, request);

        return await GetRequestByIdAsync(request.RequestId);
    }

    public async Task<Result<AssetRequestDto>> UpdateRequestAsync(long id, UpdateAssetRequestDto dto)
    {
        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Request not found.", "NOT_FOUND");
        }

        if (request.Status != RequestStatus.ReturnedForCorrection && request.Status != RequestStatus.Submitted)
        {
            return Result<AssetRequestDto>.Failure("Only requests that are Submitted or Returned For Correction can be edited.", "INVALID_STATE");
        }

        var oldStatus = request.Status;
        request.AssetCategoryId = dto.AssetCategoryId;
        request.Reason = dto.Reason.Trim();
        request.RequiredDate = dto.RequiredDate;
        request.Priority = dto.Priority;
        request.Status = RequestStatus.Submitted; // Resubmitted
        request.UpdatedAt = DateTime.UtcNow;

        _uow.AssetRequests.Update(request);

        var userId = _currentUserService.UserId;
        await _uow.RequestHistories.AddAsync(new AssetRequestHistory
        {
            RequestId = request.RequestId,
            FromStatus = oldStatus,
            ToStatus = RequestStatus.Submitted,
            ActionBy = userId,
            Remarks = "Request updated and resubmitted by employee."
        });

        await _uow.SaveChangesAsync();

        await _notificationPublisher.PublishNotificationToRoleAsync(
            "ITS_MANAGER",
            "Request Resubmitted",
            $"Request {request.RequestNumber} has been updated and resubmitted for review.",
            "Info",
            "AssetRequest",
            request.RequestId);

        return await GetRequestByIdAsync(id);
    }

    public async Task<Result<AssetRequestDto>> ValidateRequestAsync(long id, ValidateRequestDto dto)
    {
        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Request not found.", "NOT_FOUND");
        }

        var oldStatus = request.Status;
        var userId = _currentUserService.UserId;

        if (!dto.IsValid)
        {
            if (dto.ReturnForCorrection)
            {
                RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.ReturnedForCorrection);
                request.Status = RequestStatus.ReturnedForCorrection;
                request.ManagerRemarks = dto.Remarks ?? "Returned for information correction.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                {
                    return Result<AssetRequestDto>.Failure("Rejection reason is required when rejecting a request.", "REASON_REQUIRED");
                }
                RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.Rejected);
                request.Status = RequestStatus.Rejected;
                request.RejectionReason = dto.RejectionReason;
            }
        }
        else
        {
            if (dto.ExecutiveId.HasValue)
            {
                RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.AssignedToExecutive);
                request.Status = RequestStatus.AssignedToExecutive;
                request.AssignedExecutiveId = dto.ExecutiveId.Value;
                request.ManagerRemarks = dto.Remarks;
            }
            else
            {
                RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.UnderReview);
                request.Status = RequestStatus.UnderReview;
                request.ManagerRemarks = dto.Remarks;
            }
        }

        request.UpdatedAt = DateTime.UtcNow;
        _uow.AssetRequests.Update(request);

        await _uow.RequestHistories.AddAsync(new AssetRequestHistory
        {
            RequestId = request.RequestId,
            FromStatus = oldStatus,
            ToStatus = request.Status,
            ActionBy = userId,
            Remarks = dto.Remarks ?? dto.RejectionReason ?? $"Request status updated to {request.Status}."
        });

        await _uow.SaveChangesAsync();

        // Send notifications
        var empUser = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (empUser != null)
        {
            await _notificationPublisher.PublishNotificationAsync(
                empUser.UserId,
                $"Request {request.Status}",
                $"Your request {request.RequestNumber} is now {request.Status}. {dto.Remarks ?? dto.RejectionReason}".Trim(),
                request.Status == RequestStatus.Rejected ? "Error" : "Info",
                "AssetRequest",
                request.RequestId);
        }

        if (request.AssignedExecutiveId.HasValue)
        {
            await _notificationPublisher.PublishNotificationAsync(
                request.AssignedExecutiveId.Value,
                "New Task Assigned",
                $"Asset request {request.RequestNumber} has been assigned to you by the Manager.",
                "Info",
                "AssetRequest",
                request.RequestId);
        }

        await _auditService.LogAsync("Validate Request", "AssetRequest", request.RequestId.ToString(), new { Status = oldStatus }, new { Status = request.Status });

        return await GetRequestByIdAsync(id);
    }

    public async Task<Result<AssetRequestDto>> AssignExecutiveAsync(long id, AssignExecutiveDto dto)
    {
        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Request not found.", "NOT_FOUND");
        }

        var executive = await _uow.Users.GetByIdAsync(dto.ExecutiveId);
        if (executive == null || !executive.IsActive)
        {
            return Result<AssetRequestDto>.Failure("Selected ITS Executive is invalid or inactive.", "INVALID_EXECUTIVE");
        }

        var oldStatus = request.Status;
        RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.AssignedToExecutive);

        request.Status = RequestStatus.AssignedToExecutive;
        request.AssignedExecutiveId = dto.ExecutiveId;
        request.ManagerRemarks = dto.Remarks;
        request.UpdatedAt = DateTime.UtcNow;

        _uow.AssetRequests.Update(request);

        var userId = _currentUserService.UserId;
        await _uow.RequestHistories.AddAsync(new AssetRequestHistory
        {
            RequestId = request.RequestId,
            FromStatus = oldStatus,
            ToStatus = RequestStatus.AssignedToExecutive,
            ActionBy = userId,
            Remarks = $"Assigned to executive {executive.Username}. {dto.Remarks}".Trim()
        });

        await _uow.SaveChangesAsync();

        // Notify Executive and Employee
        await _notificationPublisher.PublishNotificationAsync(
            executive.UserId,
            "Task Assigned",
            $"Asset request {request.RequestNumber} has been assigned to you for processing.",
            "Info",
            "AssetRequest",
            request.RequestId);

        var empUser = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (empUser != null)
        {
            await _notificationPublisher.PublishNotificationAsync(
                empUser.UserId,
                "Request Processing",
                $"Your request {request.RequestNumber} has been assigned to an ITS Executive for fulfillment.",
                "Info",
                "AssetRequest",
                request.RequestId);
        }

        await _auditService.LogAsync("Assign Executive", "AssetRequest", request.RequestId.ToString(), null, new { request.AssignedExecutiveId, executive.Username });

        return await GetRequestByIdAsync(id);
    }

    public async Task<Result<AssetRequestDto>> RejectRequestAsync(long id, RejectRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return Result<AssetRequestDto>.Failure("A reason is required to reject a request.", "REASON_REQUIRED");
        }

        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Request not found.", "NOT_FOUND");
        }

        var oldStatus = request.Status;
        RequestStateMachine.ValidateTransition(oldStatus, RequestStatus.Rejected);

        request.Status = RequestStatus.Rejected;
        request.RejectionReason = dto.Reason.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        _uow.AssetRequests.Update(request);

        var userId = _currentUserService.UserId;
        await _uow.RequestHistories.AddAsync(new AssetRequestHistory
        {
            RequestId = request.RequestId,
            FromStatus = oldStatus,
            ToStatus = RequestStatus.Rejected,
            ActionBy = userId,
            Remarks = $"Request rejected: {dto.Reason.Trim()}"
        });

        await _uow.SaveChangesAsync();

        // Notify Employee
        var empUser = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (empUser != null)
        {
            await _notificationPublisher.PublishNotificationAsync(
                empUser.UserId,
                "Request Rejected",
                $"Your request {request.RequestNumber} was rejected: {dto.Reason.Trim()}",
                "Error",
                "AssetRequest",
                request.RequestId);
        }

        await _auditService.LogAsync("Reject Request", "AssetRequest", request.RequestId.ToString(), new { Status = oldStatus }, new { Status = RequestStatus.Rejected, dto.Reason });

        return await GetRequestByIdAsync(id);
    }

    public async Task<Result<AssetRequestDto>> AllocateRequestAsync(long id, AllocateRequestDto dto)
    {
        var request = await _uow.AssetRequests.GetByIdAsync(id);
        if (request == null)
        {
            return Result<AssetRequestDto>.Failure("Request not found.", "NOT_FOUND");
        }

        var assignResult = await _allocationService.AssignAssetAsync(dto.AssetId, new AssignAssetDto
        {
            EmployeeId = request.EmployeeId,
            RequestId = request.RequestId,
            ExpectedReturnDate = dto.ExpectedReturnDate,
            Condition = dto.Condition,
            Remarks = dto.Remarks
        });

        if (!assignResult.Succeeded)
        {
            return Result<AssetRequestDto>.Failure(assignResult.Message ?? "Failed to allocate asset.", assignResult.Code ?? "ALLOCATION_FAILED");
        }

        return await GetRequestByIdAsync(id);
    }

    private static AssetRequestDto MapToDto(
        AssetRequest r,
        Dictionary<long, Employee> employees,
        Dictionary<int, string> categories,
        Dictionary<long, User> users,
        Dictionary<long, ITAsset> assets)
    {
        employees.TryGetValue(r.EmployeeId, out var emp);
        users.TryGetValue(r.AssignedExecutiveId ?? 0, out var exec);
        assets.TryGetValue(r.AllocatedAssetId ?? 0, out var asset);

        return new AssetRequestDto
        {
            RequestId = r.RequestId,
            RequestNumber = r.RequestNumber,
            EmployeeId = r.EmployeeId,
            EmployeeCode = emp?.EmployeeCode ?? "",
            EmployeeName = emp?.FullName ?? "Unknown",
            DepartmentName = emp?.DepartmentId.HasValue == true ? "IT/Operations" : null,
            AssetCategoryId = r.AssetCategoryId,
            CategoryName = categories.TryGetValue(r.AssetCategoryId, out var cname) ? cname : "Unknown",
            Reason = r.Reason,
            RequiredDate = r.RequiredDate,
            Priority = r.Priority,
            Status = r.Status,
            AssignedExecutiveId = r.AssignedExecutiveId,
            AssignedExecutiveName = exec?.Username,
            ManagerRemarks = r.ManagerRemarks,
            RejectionReason = r.RejectionReason,
            AllocatedAssetId = r.AllocatedAssetId,
            AllocatedAssetCode = asset?.AssetCode,
            AllocatedAssetName = asset?.AssetName,
            RequestType = r.RequestType,
            CreatedAt = r.CreatedAt
        };
    }
}

public class RepairService : IRepairService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly INotificationPublisher _notificationPublisher;

    public RepairService(
        IUnitOfWork uow,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        INotificationPublisher notificationPublisher)
    {
        _uow = uow;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<PagedResult<AssetRepairDto>>> GetRepairsAsync(QueryParameters parameters, RepairStatus? status = null)
    {
        var repairs = await _uow.AssetRepairs.GetAllAsync();
        var query = repairs.AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.IssueDescription.ToLower().Contains(search) ||
                                     (assets.TryGetValue(r.AssetId, out var ast) && (ast.AssetCode.ToLower().Contains(search) || ast.AssetName.ToLower().Contains(search))));
        }

        var list = query.ToList();
        var total = list.Count;
        var items = list
            .OrderByDescending(r => r.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r =>
            {
                assets.TryGetValue(r.AssetId, out var ast);
                employees.TryGetValue(r.ReportedByEmployeeId, out var emp);
                users.TryGetValue(r.HandledByExecutiveId ?? 0, out var exec);

                return new AssetRepairDto
                {
                    RepairId = r.RepairId,
                    AssetId = r.AssetId,
                    AssetCode = ast?.AssetCode ?? "",
                    AssetName = ast?.AssetName ?? "",
                    ReportedByEmployeeId = r.ReportedByEmployeeId,
                    ReportedByEmployeeName = emp?.FullName ?? "Unknown",
                    HandledByExecutiveId = r.HandledByExecutiveId,
                    HandledByExecutiveName = exec?.Username,
                    IssueDescription = r.IssueDescription,
                    Status = r.Status,
                    VendorName = r.VendorName,
                    RepairCost = r.RepairCost,
                    SentDate = r.SentDate,
                    CompletedDate = r.CompletedDate,
                    ConditionAfterRepair = r.ConditionAfterRepair,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

        return Result<PagedResult<AssetRepairDto>>.Success(new PagedResult<AssetRepairDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<AssetRepairDto>> GetRepairByIdAsync(long id)
    {
        var r = await _uow.AssetRepairs.GetByIdAsync(id);
        if (r == null)
        {
            return Result<AssetRepairDto>.Failure("Repair record not found.", "NOT_FOUND");
        }

        var ast = await _uow.Assets.GetByIdAsync(r.AssetId);
        var emp = await _uow.Employees.GetByIdAsync(r.ReportedByEmployeeId);
        var exec = r.HandledByExecutiveId.HasValue ? await _uow.Users.GetByIdAsync(r.HandledByExecutiveId.Value) : null;

        return Result<AssetRepairDto>.Success(new AssetRepairDto
        {
            RepairId = r.RepairId,
            AssetId = r.AssetId,
            AssetCode = ast?.AssetCode ?? "",
            AssetName = ast?.AssetName ?? "",
            ReportedByEmployeeId = r.ReportedByEmployeeId,
            ReportedByEmployeeName = emp?.FullName ?? "Unknown",
            HandledByExecutiveId = r.HandledByExecutiveId,
            HandledByExecutiveName = exec?.Username,
            IssueDescription = r.IssueDescription,
            Status = r.Status,
            VendorName = r.VendorName,
            RepairCost = r.RepairCost,
            SentDate = r.SentDate,
            CompletedDate = r.CompletedDate,
            ConditionAfterRepair = r.ConditionAfterRepair,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<Result<AssetRepairDto>> CreateRepairAsync(CreateRepairDto dto)
    {
        var asset = await _uow.Assets.GetByIdAsync(dto.AssetId);
        if (asset == null)
        {
            return Result<AssetRepairDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var empId = _currentUserService.EmployeeId ?? asset.CurrentEmployeeId;
        if (!empId.HasValue)
        {
            return Result<AssetRepairDto>.Failure("Cannot determine reporting employee.", "INVALID_EMPLOYEE");
        }

        var repair = new AssetRepair
        {
            AssetId = asset.AssetId,
            ReportedByEmployeeId = empId.Value,
            IssueDescription = dto.IssueDescription.Trim(),
            Status = RepairStatus.Reported,
            ConditionAfterRepair = null
        };

        // Mark asset as UnderMaintenance
        if (asset.Status != AssetStatus.UnderMaintenance && asset.Status != AssetStatus.InRepair)
        {
            asset.Status = AssetStatus.UnderMaintenance;
            asset.Condition = AssetCondition.NotWorking;
            _uow.Assets.Update(asset);
        }

        await _uow.AssetRepairs.AddAsync(repair);
        await _uow.SaveChangesAsync();

        await _notificationPublisher.PublishNotificationToRoleAsync(
            "ITS_MANAGER",
            "New Repair Issue Reported",
            $"Issue reported on asset {asset.AssetCode}: {dto.IssueDescription}",
            "Warning",
            "AssetRepair",
            repair.RepairId);

        await _auditService.LogAsync("Create Repair Request", "AssetRepair", repair.RepairId.ToString(), null, repair);

        return await GetRepairByIdAsync(repair.RepairId);
    }

    public async Task<Result<AssetRepairDto>> UpdateRepairStatusAsync(long id, UpdateRepairStatusDto dto)
    {
        var repair = await _uow.AssetRepairs.GetByIdAsync(id);
        if (repair == null)
        {
            return Result<AssetRepairDto>.Failure("Repair record not found.", "NOT_FOUND");
        }

        var asset = await _uow.Assets.GetByIdAsync(repair.AssetId);
        var oldStatus = repair.Status;

        repair.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.VendorName)) repair.VendorName = dto.VendorName.Trim();
        if (dto.RepairCost.HasValue) repair.RepairCost = dto.RepairCost.Value;
        if (dto.SentDate.HasValue) repair.SentDate = dto.SentDate.Value;
        if (dto.CompletedDate.HasValue) repair.CompletedDate = dto.CompletedDate.Value;
        if (dto.ConditionAfterRepair.HasValue) repair.ConditionAfterRepair = dto.ConditionAfterRepair.Value;
        if (_currentUserService.UserId.HasValue) repair.HandledByExecutiveId = _currentUserService.UserId.Value;

        repair.UpdatedAt = DateTime.UtcNow;
        _uow.AssetRepairs.Update(repair);

        if (asset != null)
        {
            if (dto.Status == RepairStatus.InRepair)
            {
                asset.Status = AssetStatus.InRepair;
            }
            else if (dto.Status == RepairStatus.Completed)
            {
                asset.Status = dto.ResultingAssetStatus ?? AssetStatus.Available;
                asset.Condition = dto.ConditionAfterRepair ?? AssetCondition.Good;
            }
            else if (dto.Status == RepairStatus.ClosedNoRepair)
            {
                asset.Status = dto.ResultingAssetStatus ?? AssetStatus.Available;
            }
            asset.UpdatedAt = DateTime.UtcNow;
            _uow.Assets.Update(asset);
        }

        await _uow.SaveChangesAsync();

        // Notify reporting employee
        var empUser = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == repair.ReportedByEmployeeId);
        if (empUser != null)
        {
            await _notificationPublisher.PublishNotificationAsync(
                empUser.UserId,
                "Repair Status Updated",
                $"Repair request for {asset?.AssetCode} is now {dto.Status}.",
                "Info",
                "AssetRepair",
                repair.RepairId);
        }

        await _auditService.LogAsync("Update Repair Status", "AssetRepair", repair.RepairId.ToString(), new { Status = oldStatus }, new { Status = dto.Status });

        return await GetRepairByIdAsync(id);
    }
}

public class ReplacementService : IReplacementService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAllocationService _allocationService;
    private readonly IAuditService _auditService;
    private readonly INotificationPublisher _notificationPublisher;

    public ReplacementService(
        IUnitOfWork uow,
        ICurrentUserService currentUserService,
        IAllocationService allocationService,
        IAuditService auditService,
        INotificationPublisher notificationPublisher)
    {
        _uow = uow;
        _currentUserService = currentUserService;
        _allocationService = allocationService;
        _auditService = auditService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<PagedResult<AssetReplacementDto>>> GetReplacementsAsync(QueryParameters parameters, ReplacementStatus? status = null)
    {
        var replacements = await _uow.AssetReplacements.GetAllAsync();
        var query = replacements.AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);

        var list = query.ToList();
        var total = list.Count;
        var items = list
            .OrderByDescending(r => r.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r =>
            {
                assets.TryGetValue(r.OldAssetId, out var oldAst);
                assets.TryGetValue(r.NewAssetId ?? 0, out var newAst);
                employees.TryGetValue(r.EmployeeId, out var emp);
                users.TryGetValue(r.ApprovedBy ?? 0, out var appUser);

                return new AssetReplacementDto
                {
                    ReplacementId = r.ReplacementId,
                    OldAssetId = r.OldAssetId,
                    OldAssetCode = oldAst?.AssetCode ?? "",
                    OldAssetName = oldAst?.AssetName ?? "",
                    NewAssetId = r.NewAssetId,
                    NewAssetCode = newAst?.AssetCode,
                    NewAssetName = newAst?.AssetName,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = emp?.FullName ?? "Unknown",
                    Reason = r.Reason,
                    Status = r.Status,
                    ApprovedBy = r.ApprovedBy,
                    ApprovedByName = appUser?.Username,
                    RejectionReason = r.RejectionReason,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

        return Result<PagedResult<AssetReplacementDto>>.Success(new PagedResult<AssetReplacementDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }

    public async Task<Result<AssetReplacementDto>> GetReplacementByIdAsync(long id)
    {
        var r = await _uow.AssetReplacements.GetByIdAsync(id);
        if (r == null)
        {
            return Result<AssetReplacementDto>.Failure("Replacement request not found.", "NOT_FOUND");
        }

        var oldAst = await _uow.Assets.GetByIdAsync(r.OldAssetId);
        var newAst = r.NewAssetId.HasValue ? await _uow.Assets.GetByIdAsync(r.NewAssetId.Value) : null;
        var emp = await _uow.Employees.GetByIdAsync(r.EmployeeId);
        var appUser = r.ApprovedBy.HasValue ? await _uow.Users.GetByIdAsync(r.ApprovedBy.Value) : null;

        return Result<AssetReplacementDto>.Success(new AssetReplacementDto
        {
            ReplacementId = r.ReplacementId,
            OldAssetId = r.OldAssetId,
            OldAssetCode = oldAst?.AssetCode ?? "",
            OldAssetName = oldAst?.AssetName ?? "",
            NewAssetId = r.NewAssetId,
            NewAssetCode = newAst?.AssetCode,
            NewAssetName = newAst?.AssetName,
            EmployeeId = r.EmployeeId,
            EmployeeName = emp?.FullName ?? "Unknown",
            Reason = r.Reason,
            Status = r.Status,
            ApprovedBy = r.ApprovedBy,
            ApprovedByName = appUser?.Username,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<Result<AssetReplacementDto>> CreateReplacementAsync(CreateReplacementDto dto)
    {
        var oldAsset = await _uow.Assets.GetByIdAsync(dto.OldAssetId);
        if (oldAsset == null)
        {
            return Result<AssetReplacementDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var empId = _currentUserService.EmployeeId ?? oldAsset.CurrentEmployeeId;
        if (!empId.HasValue)
        {
            return Result<AssetReplacementDto>.Failure("Cannot determine requesting employee.", "INVALID_EMPLOYEE");
        }

        var replacement = new AssetReplacement
        {
            OldAssetId = oldAsset.AssetId,
            EmployeeId = empId.Value,
            Reason = dto.Reason.Trim(),
            Status = ReplacementStatus.Requested
        };

        await _uow.AssetReplacements.AddAsync(replacement);
        await _uow.SaveChangesAsync();

        await _notificationPublisher.PublishNotificationToRoleAsync(
            "ITS_MANAGER",
            "Replacement Requested",
            $"Replacement requested for asset {oldAsset.AssetCode}: {dto.Reason}",
            "Info",
            "AssetReplacement",
            replacement.ReplacementId);

        await _auditService.LogAsync("Create Replacement Request", "AssetReplacement", replacement.ReplacementId.ToString(), null, replacement);

        return await GetReplacementByIdAsync(replacement.ReplacementId);
    }

    public async Task<Result<AssetReplacementDto>> ProcessReplacementAsync(
     long id,
     ReplacementDecisionDto dto)
    {
        var replacement = await _uow.AssetReplacements.GetByIdAsync(id);

        if (replacement == null)
        {
            return Result<AssetReplacementDto>.Failure(
                "Replacement request not found.",
                "NOT_FOUND");
        }

        var userId = _currentUserService.UserId ?? 1;

        // ============================================================
        // REJECT
        // ============================================================

        if (!dto.Approved)
        {
            replacement.Status = ReplacementStatus.Rejected;
            replacement.RejectionReason =
                dto.RejectionReason ?? "Replacement rejected by Manager.";
            replacement.ApprovedBy = userId;
            replacement.UpdatedAt = DateTime.UtcNow;

            _uow.AssetReplacements.Update(replacement);

            await _uow.SaveChangesAsync();

            var empUser = await _uow.Users.FirstOrDefaultAsync(
                u => u.EmployeeId == replacement.EmployeeId);

            if (empUser != null)
            {
                await _notificationPublisher.PublishNotificationAsync(
                    empUser.UserId,
                    "Replacement Rejected",
                    $"Your replacement request was rejected: {replacement.RejectionReason}",
                    "Error",
                    "AssetReplacement",
                    replacement.ReplacementId);
            }

            return await GetReplacementByIdAsync(id);
        }

        // ============================================================
        // APPROVE - NEW ASSET REQUIRED
        // ============================================================

        if (!dto.NewAssetId.HasValue)
        {
            return Result<AssetReplacementDto>.Failure(
                "New replacement asset must be selected for approval.",
                "NEW_ASSET_REQUIRED");
        }

        // ============================================================
        // GET OLD ASSET
        // ============================================================

        var oldAsset = await _uow.Assets.GetByIdAsync(
            replacement.OldAssetId);

        if (oldAsset == null)
        {
            return Result<AssetReplacementDto>.Failure(
                "Original asset associated with this replacement was not found.",
                "OLD_ASSET_NOT_FOUND");
        }

        // ============================================================
        // GET NEW ASSET
        // ============================================================

        var newAsset = await _uow.Assets.GetByIdAsync(
            dto.NewAssetId.Value);

        if (newAsset == null)
        {
            return Result<AssetReplacementDto>.Failure(
                "Selected replacement asset was not found.",
                "NEW_ASSET_NOT_FOUND");
        }

        if (newAsset.Status != AssetStatus.Available)
        {
            return Result<AssetReplacementDto>.Failure(
                $"Selected asset {newAsset.AssetCode} is not available. " +
                $"Current status: {newAsset.Status}.",
                "INVALID_ASSET");
        }

        // Prevent selecting the same asset
        if (newAsset.AssetId == oldAsset.AssetId)
        {
            return Result<AssetReplacementDto>.Failure(
                "The replacement asset cannot be the same as the old asset.",
                "SAME_ASSET");
        }

        // ============================================================
        // APPROVE AND PROCESS REPLACEMENT
        // ============================================================

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var oldAllocation =
                    await _uow.AssetAllocations.FirstOrDefaultAsync(
                        a =>
                            a.AssetId == replacement.OldAssetId &&
                            a.EmployeeId == replacement.EmployeeId &&
                            a.AllocationStatus == AllocationStatus.Active);

                if (oldAllocation != null)
                {
                    oldAllocation.AllocationStatus =
                        AllocationStatus.Returned;

                    oldAllocation.ReturnDate = DateTime.UtcNow;
                    oldAllocation.Remarks =
                        "Returned due to asset replacement.";

                    _uow.AssetAllocations.Update(oldAllocation);
                }

                // Mark old asset as replaced
                oldAsset.Status = AssetStatus.Replaced;
                oldAsset.CurrentEmployeeId = null;

                _uow.Assets.Update(oldAsset);

                // Create allocation for new asset
                var newAllocation = new AssetAllocation
                {
                    AssetId = newAsset.AssetId,
                    EmployeeId = replacement.EmployeeId,
                    AllocatedBy = userId,
                    AllocationDate = DateTime.UtcNow,
                    AllocationStatus = AllocationStatus.Active,
                    ConditionAtAllocation = newAsset.Condition,
                    Remarks =
                        $"Allocated as replacement for {oldAsset.AssetCode}."
                };

                await _uow.AssetAllocations.AddAsync(newAllocation);

                // Assign new asset
                newAsset.Status = AssetStatus.Assigned;
                newAsset.CurrentEmployeeId = replacement.EmployeeId;

                _uow.Assets.Update(newAsset);

                // Complete replacement
                replacement.Status = ReplacementStatus.Completed;
                replacement.NewAssetId = newAsset.AssetId;
                replacement.ApprovedBy = userId;
                replacement.UpdatedAt = DateTime.UtcNow;

                _uow.AssetReplacements.Update(replacement);
            });
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error processing replacement decision. " +
                "ReplacementId: {ReplacementId}, " +
                "NewAssetId: {NewAssetId}, " +
                "Approved: {Approved}",
                id,
                dto.NewAssetId,
                dto.Approved);

            throw;
        }

        // ============================================================
        // NOTIFICATION
        // ============================================================

        var employeeUser = await _uow.Users.FirstOrDefaultAsync(
            u => u.EmployeeId == replacement.EmployeeId);

        if (employeeUser != null)
        {
            await _notificationPublisher.PublishNotificationAsync(
                employeeUser.UserId,
                "Asset Replacement Approved",
                $"Your asset replacement has been approved. " +
                $"New asset: {newAsset.AssetCode}",
                "Success",
                "AssetReplacement",
                replacement.ReplacementId);
        }

        // ============================================================
        // AUDIT LOG
        // ============================================================

        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "AssetReplacementApproved",
            EntityName = "AssetReplacement",
            EntityId = replacement.ReplacementId.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        return await GetReplacementByIdAsync(id);
    }
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(IUnitOfWork uow, ICurrentUserService currentUserService)
    {
        _uow = uow;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<NotificationDto>>> GetNotificationsAsync()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<List<NotificationDto>>.Success(new List<NotificationDto>());
        }

        var list = (await _uow.Notifications.FindAsync(n => n.UserId == userId.Value))
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                RelatedEntity = n.RelatedEntity,
                RelatedId = n.RelatedId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

        return Result<List<NotificationDto>>.Success(list);
    }

    public async Task<Result> MarkAsReadAsync(long id)
    {
        var notif = await _uow.Notifications.GetByIdAsync(id);
        if (notif != null && _currentUserService.UserId == notif.UserId)
        {
            notif.IsRead = true;
            _uow.Notifications.Update(notif);
            await _uow.SaveChangesAsync();
        }
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue)
        {
            var unread = await _uow.Notifications.FindAsync(n => n.UserId == userId.Value && !n.IsRead);
            foreach (var n in unread)
            {
                n.IsRead = true;
                _uow.Notifications.Update(n);
            }
            await _uow.SaveChangesAsync();
        }
        return Result.Success();
    }
}

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<byte[]> GenerateAssetReportCsvAsync()
    {
        var assets = await _uow.Assets.GetAllAsync();
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);

        var sb = new StringBuilder();
        sb.AppendLine("AssetId,AssetCode,AssetName,Category,SerialNumber,Brand,Model,PurchaseCost(INR),PurchaseDate,WarrantyExpiryDate,Status,Condition,Location,AssignedTo");

        foreach (var a in assets)
        {
            var catName = categories.TryGetValue(a.AssetCategoryId, out var c) ? c : "";
            Employee? emp = null;
            if (a.CurrentEmployeeId.HasValue) employees.TryGetValue(a.CurrentEmployeeId.Value, out emp);

            sb.AppendLine($"\"{a.AssetId}\",\"{a.AssetCode}\",\"{EscapeCsv(a.AssetName)}\",\"{catName}\",\"{a.SerialNumber}\",\"{a.Brand}\",\"{a.Model}\",\"{a.PurchaseCost:F2}\",\"{a.PurchaseDate:yyyy-MM-dd}\",\"{a.WarrantyExpiryDate:yyyy-MM-dd}\",\"{a.Status}\",\"{a.Condition}\",\"{a.Location}\",\"{EscapeCsv(emp?.FullName ?? "")}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GenerateAllocationReportCsvAsync()
    {
        var allocations = await _uow.AssetAllocations.GetAllAsync();
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);

        var sb = new StringBuilder();
        sb.AppendLine("AllocationId,AssetCode,AssetName,EmployeeCode,EmployeeName,AllocationDate,ExpectedReturnDate,ReturnDate,Status,AllocatedBy,Remarks");

        foreach (var al in allocations)
        {
            assets.TryGetValue(al.AssetId, out var ast);
            employees.TryGetValue(al.EmployeeId, out var emp);
            users.TryGetValue(al.AllocatedBy, out var user);

            sb.AppendLine($"\"{al.AllocationId}\",\"{ast?.AssetCode}\",\"{EscapeCsv(ast?.AssetName ?? "")}\",\"{emp?.EmployeeCode}\",\"{EscapeCsv(emp?.FullName ?? "")}\",\"{al.AllocationDate:yyyy-MM-dd HH:mm}\",\"{al.ExpectedReturnDate:yyyy-MM-dd}\",\"{al.ReturnDate:yyyy-MM-dd HH:mm}\",\"{al.AllocationStatus}\",\"{user?.Username}\",\"{EscapeCsv(al.Remarks ?? "")}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GenerateRepairReportCsvAsync()
    {
        var repairs = await _uow.AssetRepairs.GetAllAsync();
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);

        var sb = new StringBuilder();
        sb.AppendLine("RepairId,AssetCode,AssetName,ReportedBy,IssueDescription,Status,VendorName,RepairCost(INR),SentDate,CompletedDate");

        foreach (var r in repairs)
        {
            assets.TryGetValue(r.AssetId, out var ast);
            employees.TryGetValue(r.ReportedByEmployeeId, out var emp);

            sb.AppendLine($"\"{r.RepairId}\",\"{ast?.AssetCode}\",\"{EscapeCsv(ast?.AssetName ?? "")}\",\"{EscapeCsv(emp?.FullName ?? "")}\",\"{EscapeCsv(r.IssueDescription)}\",\"{r.Status}\",\"{EscapeCsv(r.VendorName ?? "")}\",\"{r.RepairCost:F2}\",\"{r.SentDate:yyyy-MM-dd}\",\"{r.CompletedDate:yyyy-MM-dd}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<object> GetAssetReportDataAsync()
    {
        var assets = await _uow.Assets.GetAllAsync();
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);

        return assets.Select(a => new
        {
            a.AssetId,
            a.AssetCode,
            a.AssetName,
            Category = categories.TryGetValue(a.AssetCategoryId, out var c) ? c : "",
            a.SerialNumber,
            a.Brand,
            a.PurchaseCost,
            Status = a.Status.ToString(),
            Condition = a.Condition.ToString(),
            AssignedTo = a.CurrentEmployeeId.HasValue && employees.TryGetValue(a.CurrentEmployeeId.Value, out var emp) ? emp.FullName : "-"
        });
    }

    public async Task<object> GetAllocationReportDataAsync()
    {
        var allocations = await _uow.AssetAllocations.GetAllAsync();
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);

        return allocations.OrderByDescending(a => a.AllocationDate).Select(a => new
        {
            a.AllocationId,
            AssetCode = assets.TryGetValue(a.AssetId, out var ast) ? ast.AssetCode : "",
            AssetName = ast?.AssetName ?? "",
            EmployeeName = employees.TryGetValue(a.EmployeeId, out var emp) ? emp.FullName : "",
            EmployeeCode = emp?.EmployeeCode ?? "",
            AllocationDate = a.AllocationDate.ToString("yyyy-MM-dd"),
            ReturnDate = a.ReturnDate?.ToString("yyyy-MM-dd"),
            Status = a.AllocationStatus.ToString()
        });
    }

    public async Task<object> GetRepairReportDataAsync()
    {
        var repairs = await _uow.AssetRepairs.GetAllAsync();
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);

        return repairs.OrderByDescending(r => r.CreatedAt).Select(r => new
        {
            r.RepairId,
            AssetCode = assets.TryGetValue(r.AssetId, out var ast) ? ast.AssetCode : "",
            AssetName = ast?.AssetName ?? "",
            ReportedBy = employees.TryGetValue(r.ReportedByEmployeeId, out var emp) ? emp.FullName : "",
            r.IssueDescription,
            Status = r.Status.ToString(),
            r.VendorName,
            r.RepairCost
        });
    }

    private static string EscapeCsv(string str) => str.Replace("\"", "\"\"");
}
