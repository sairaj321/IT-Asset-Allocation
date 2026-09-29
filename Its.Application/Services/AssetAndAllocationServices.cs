using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Interfaces;
using Its.Application.Strategies;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Domain.Exceptions;
using Its.Domain.State;

namespace Its.Application.Services;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly AssetValidationPipeline _validationPipeline;

    public AssetService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
        _validationPipeline = new AssetValidationPipeline();
    }

    public async Task<Result<PagedResult<AssetDto>>> GetAssetsAsync(AssetFilterDto filter)
    {
        var allAssets = await _uow.Assets.GetAllAsync();
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var activeAllocations = await _uow.AssetAllocations.FindAsync(a => a.AllocationStatus == AllocationStatus.Active);

        var query = allAssets.AsEnumerable();

        if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
        {
            query = query.Where(a => a.AssetCategoryId == filter.CategoryId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (filter.Condition.HasValue)
        {
            query = query.Where(a => a.Condition == filter.Condition.Value);
        }

        if (filter.ExpiringWarranty.HasValue && filter.ExpiringWarranty.Value)
        {
            var threshold = DateTime.UtcNow.AddDays(30);
            query = query.Where(a => a.WarrantyExpiryDate.HasValue && a.WarrantyExpiryDate.Value <= threshold);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(a => a.AssetCode.ToLower().Contains(search) ||
                                     a.AssetName.ToLower().Contains(search) ||
                                     a.SerialNumber.ToLower().Contains(search) ||
                                     (a.Brand != null && a.Brand.ToLower().Contains(search)) ||
                                     (a.Model != null && a.Model.ToLower().Contains(search)));
        }

        var list = query.ToList();
        var total = list.Count;

        var items = list
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a =>
            {
                var alloc = activeAllocations.FirstOrDefault(x => x.AssetId == a.AssetId);
                Employee? emp = null;
                if (a.CurrentEmployeeId.HasValue)
                {
                    employees.TryGetValue(a.CurrentEmployeeId.Value, out emp);
                }

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
                    AssignedEmployeeCode = emp?.EmployeeCode,
                    AssignedEmployeeName = emp?.FullName,
                    AllocationDate = alloc?.AllocationDate,
                    ExpectedReturnDate = alloc?.ExpectedReturnDate,
                    IsActive = a.IsActive,
                    Version = a.Version,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();

        return Result<PagedResult<AssetDto>>.Success(new PagedResult<AssetDto>(items, total, filter.PageNumber, filter.PageSize));
    }

    public async Task<Result<AssetDto>> GetAssetByIdAsync(long id)
    {
        var asset = await _uow.Assets.GetByIdAsync(id);
        if (asset == null)
        {
            return Result<AssetDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var category = await _uow.AssetCategories.GetByIdAsync(asset.AssetCategoryId);
        Employee? employee = null;
        if (asset.CurrentEmployeeId.HasValue)
        {
            employee = await _uow.Employees.GetByIdAsync(asset.CurrentEmployeeId.Value);
        }

        var alloc = await _uow.AssetAllocations.FirstOrDefaultAsync(a => a.AssetId == id && a.AllocationStatus == AllocationStatus.Active);

        return Result<AssetDto>.Success(new AssetDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            AssetCategoryId = asset.AssetCategoryId,
            CategoryName = category?.CategoryName ?? "Unknown",
            SerialNumber = asset.SerialNumber,
            Brand = asset.Brand,
            Model = asset.Model,
            PurchaseDate = asset.PurchaseDate,
            PurchaseCost = asset.PurchaseCost,
            WarrantyStartDate = asset.WarrantyStartDate,
            WarrantyExpiryDate = asset.WarrantyExpiryDate,
            Status = asset.Status,
            Condition = asset.Condition,
            Location = asset.Location,
            CurrentEmployeeId = asset.CurrentEmployeeId,
            AssignedEmployeeCode = employee?.EmployeeCode,
            AssignedEmployeeName = employee?.FullName,
            AllocationDate = alloc?.AllocationDate,
            ExpectedReturnDate = alloc?.ExpectedReturnDate,
            IsActive = asset.IsActive,
            Version = asset.Version,
            CreatedAt = asset.CreatedAt
        });
    }

    public async Task<Result<AssetDto>> CreateAssetAsync(CreateAssetDto dto)
    {
        var existing = await _uow.Assets.FirstOrDefaultAsync(a => a.AssetCode.ToLower() == dto.AssetCode.Trim().ToLower() || a.SerialNumber.ToLower() == dto.SerialNumber.Trim().ToLower());
        if (existing != null)
        {
            return Result<AssetDto>.Failure("Asset code or serial number already exists.", "DUPLICATE_ASSET");
        }

        var category = await _uow.AssetCategories.GetByIdAsync(dto.AssetCategoryId);
        if (category == null)
        {
            return Result<AssetDto>.Failure("Asset category does not exist.", "NOT_FOUND");
        }

        var asset = new ITAsset
        {
            AssetCode = dto.AssetCode.Trim().ToUpper(),
            AssetName = dto.AssetName.Trim(),
            AssetCategoryId = dto.AssetCategoryId,
            SerialNumber = dto.SerialNumber.Trim().ToUpper(),
            Brand = dto.Brand?.Trim(),
            Model = dto.Model?.Trim(),
            PurchaseDate = dto.PurchaseDate,
            PurchaseCost = dto.PurchaseCost,
            WarrantyStartDate = dto.WarrantyStartDate,
            WarrantyExpiryDate = dto.WarrantyExpiryDate,
            Status = AssetStatus.Available,
            Condition = dto.Condition,
            Location = dto.Location?.Trim(),
            IsActive = true
        };

        await _uow.Assets.AddAsync(asset);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Create Asset", "ITAsset", asset.AssetId.ToString(), null, asset);

        return await GetAssetByIdAsync(asset.AssetId);
    }

    public async Task<Result<AssetDto>> UpdateAssetAsync(long id, UpdateAssetDto dto)
    {
        var asset = await _uow.Assets.GetByIdAsync(id);
        if (asset == null)
        {
            return Result<AssetDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var oldVal = new { asset.AssetName, asset.Status, asset.Condition, asset.Location, asset.PurchaseCost };

        // Validate status transition if status is being modified
        if (asset.Status != dto.Status)
        {
            AssetStateMachine.ValidateTransition(asset.Status, dto.Status);
            asset.Status = dto.Status;
        }

        asset.AssetName = dto.AssetName.Trim();
        asset.AssetCategoryId = dto.AssetCategoryId;
        asset.SerialNumber = dto.SerialNumber.Trim().ToUpper();
        asset.Brand = dto.Brand?.Trim();
        asset.Model = dto.Model?.Trim();
        asset.PurchaseDate = dto.PurchaseDate;
        asset.PurchaseCost = dto.PurchaseCost;
        asset.WarrantyStartDate = dto.WarrantyStartDate;
        asset.WarrantyExpiryDate = dto.WarrantyExpiryDate;
        asset.Condition = dto.Condition;
        asset.Location = dto.Location?.Trim();
        asset.UpdatedAt = DateTime.UtcNow;

        _uow.Assets.Update(asset);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Update Asset", "ITAsset", asset.AssetId.ToString(), oldVal, asset);

        return await GetAssetByIdAsync(id);
    }

    public async Task<Result> DeactivateAssetAsync(long id)
    {
        var asset = await _uow.Assets.GetByIdAsync(id);
        if (asset == null)
        {
            return Result.Failure("Asset not found.", "NOT_FOUND");
        }

        if (asset.Status == AssetStatus.Assigned)
        {
            return Result.Failure("Cannot deactivate an asset that is currently assigned. Return the asset first.", "CONFLICT");
        }

        asset.IsActive = false;
        asset.Status = AssetStatus.Retired;
        asset.UpdatedAt = DateTime.UtcNow;

        _uow.Assets.Update(asset);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Deactivate Asset", "ITAsset", asset.AssetId.ToString());

        return Result.Success("Asset deactivated successfully.");
    }

    public async Task<Result<List<AssetDto>>> GetAvailableAssetsAsync(int? categoryId = null)
    {
        var assets = await _uow.Assets.FindAsync(a => a.IsActive && a.Status == AssetStatus.Available && (!categoryId.HasValue || a.AssetCategoryId == categoryId.Value));
        var categories = (await _uow.AssetCategories.GetAllAsync()).ToDictionary(c => c.AssetCategoryId, c => c.CategoryName);

        var list = assets.Select(a => new AssetDto
        {
            AssetId = a.AssetId,
            AssetCode = a.AssetCode,
            AssetName = a.AssetName,
            AssetCategoryId = a.AssetCategoryId,
            CategoryName = categories.TryGetValue(a.AssetCategoryId, out var cname) ? cname : "",
            SerialNumber = a.SerialNumber,
            Brand = a.Brand,
            Model = a.Model,
            PurchaseCost = a.PurchaseCost,
            WarrantyStartDate = a.WarrantyStartDate,
            WarrantyExpiryDate = a.WarrantyExpiryDate,
            Status = a.Status,
            Condition = a.Condition,
            Location = a.Location,
            IsActive = a.IsActive
        }).ToList();

        return Result<List<AssetDto>>.Success(list);
    }

    public async Task<Result<AssetCheckResult>> CheckAssetAsync(long assetId, long? employeeId = null)
    {
        var asset = await _uow.Assets.GetByIdAsync(assetId);
        if (asset == null)
        {
            return Result<AssetCheckResult>.Failure("Asset not found.", "NOT_FOUND");
        }

        Employee? employee = null;
        if (employeeId.HasValue)
        {
            employee = await _uow.Employees.GetByIdAsync(employeeId.Value);
        }

        var checkResult = _validationPipeline.Execute(asset, employee);
        return Result<AssetCheckResult>.Success(checkResult);
    }

    public async Task<Result<List<AssetCategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _uow.AssetCategories.GetAllAsync();
        var allAssets = await _uow.Assets.GetAllAsync();

        var dtos = categories.Select(c => new AssetCategoryDto
        {
            AssetCategoryId = c.AssetCategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            IsActive = c.IsActive,
            TotalAssetsCount = allAssets.Count(a => a.AssetCategoryId == c.AssetCategoryId && a.IsActive),
            AvailableAssetsCount = allAssets.Count(a => a.AssetCategoryId == c.AssetCategoryId && a.IsActive && a.Status == AssetStatus.Available)
        }).ToList();

        return Result<List<AssetCategoryDto>>.Success(dtos);
    }

    public async Task<Result<AssetCategoryDto>> CreateCategoryAsync(AssetCategoryDto dto)
    {
        var existing = await _uow.AssetCategories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == dto.CategoryName.Trim().ToLower());
        if (existing != null)
        {
            return Result<AssetCategoryDto>.Failure("Category name already exists.", "DUPLICATE");
        }

        var cat = new AssetCategory
        {
            CategoryName = dto.CategoryName.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true
        };

        await _uow.AssetCategories.AddAsync(cat);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Create Category", "AssetCategory", cat.AssetCategoryId.ToString(), null, cat);

        return Result<AssetCategoryDto>.Success(new AssetCategoryDto
        {
            AssetCategoryId = cat.AssetCategoryId,
            CategoryName = cat.CategoryName,
            Description = cat.Description,
            IsActive = cat.IsActive
        });
    }

    public async Task<Result<AssetCategoryDto>> UpdateCategoryAsync(int id, AssetCategoryDto dto)
    {
        var cat = await _uow.AssetCategories.GetByIdAsync(id);
        if (cat == null)
        {
            return Result<AssetCategoryDto>.Failure("Category not found.", "NOT_FOUND");
        }

        cat.CategoryName = dto.CategoryName.Trim();
        cat.Description = dto.Description?.Trim();
        cat.IsActive = dto.IsActive;
        cat.UpdatedAt = DateTime.UtcNow;

        _uow.AssetCategories.Update(cat);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Update Category", "AssetCategory", id.ToString(), null, cat);

        return Result<AssetCategoryDto>.Success(new AssetCategoryDto
        {
            AssetCategoryId = cat.AssetCategoryId,
            CategoryName = cat.CategoryName,
            Description = cat.Description,
            IsActive = cat.IsActive
        });
    }
}

public class AllocationService : IAllocationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly AssetValidationPipeline _validationPipeline;

    public AllocationService(
        IUnitOfWork uow,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        INotificationPublisher notificationPublisher)
    {
        _uow = uow;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _notificationPublisher = notificationPublisher;
        _validationPipeline = new AssetValidationPipeline();
    }

    public async Task<Result<AssetDto>> AssignAssetAsync(
     long assetId,
     AssignAssetDto dto)
    {
        var asset = await _uow.Assets.GetByIdAsync(assetId);

        if (asset == null)
        {
            return Result<AssetDto>.Failure(
                "Asset not found.",
                "NOT_FOUND");
        }

        var employee = await _uow.Employees.GetByIdAsync(dto.EmployeeId);

        if (employee == null)
        {
            return Result<AssetDto>.Failure(
                "Employee not found.",
                "NOT_FOUND");
        }

        // Run validation pipeline
        var checkResult = _validationPipeline.Execute(asset, employee);

        if (!checkResult.IsEligible)
        {
            var firstFailure =
                checkResult.Checks.FirstOrDefault(c => !c.Passed);

            return Result<AssetDto>.Failure(
                firstFailure?.Message ??
                "Asset is not eligible for allocation.",
                "VALIDATION_FAILED");
        }

        var userId = _currentUserService.UserId ?? 1;

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                // ========================================================
                // 1. CREATE ASSET ALLOCATION
                // ========================================================

                var allocation = new AssetAllocation
                {
                    AssetId = asset.AssetId,
                    EmployeeId = employee.EmployeeId,
                    RequestId = dto.RequestId,
                    AllocatedBy = userId,
                    AllocationDate = DateTime.UtcNow,
                    ExpectedReturnDate = dto.ExpectedReturnDate,
                    AllocationStatus = AllocationStatus.Active,
                    ConditionAtAllocation = dto.Condition,
                    Remarks = dto.Remarks
                };

                await _uow.AssetAllocations.AddAsync(allocation);

                // ========================================================
                // 2. UPDATE ASSET
                // ========================================================

                asset.Status = AssetStatus.Assigned;
                asset.CurrentEmployeeId = employee.EmployeeId;
                asset.Condition = dto.Condition;
                asset.UpdatedAt = DateTime.UtcNow;

                _uow.Assets.Update(asset);

                // ========================================================
                // 3. UPDATE REQUEST
                // ========================================================

                if (dto.RequestId.HasValue)
                {
                    var request =
                        await _uow.AssetRequests.GetByIdAsync(
                            dto.RequestId.Value);

                    if (request != null)
                    {
                        RequestStateMachine.ValidateTransition(
                            request.Status,
                            RequestStatus.Allocated);

                        request.Status = RequestStatus.Allocated;
                        request.AllocatedAssetId = asset.AssetId;
                        request.UpdatedAt = DateTime.UtcNow;

                        _uow.AssetRequests.Update(request);

                        await _uow.RequestHistories.AddAsync(
                            new AssetRequestHistory
                            {
                                RequestId = request.RequestId,
                                FromStatus = request.Status,
                                ToStatus = RequestStatus.Allocated,
                                ActionBy = userId,
                                Remarks =
                                    $"Asset {asset.AssetCode} " +
                                    $"({asset.AssetName}) allocated " +
                                    $"to {employee.FullName}."
                            });
                    }
                }

                // DO NOT call SaveChangesAsync() here.
                // ExecuteInTransactionAsync() handles it.
            });

            // ============================================================
            // 4. NOTIFY EMPLOYEE
            // ============================================================

            var empUser =
                await _uow.Users.FirstOrDefaultAsync(
                    u => u.EmployeeId == employee.EmployeeId);

            if (empUser != null)
            {
                await _notificationPublisher.PublishNotificationAsync(
                    empUser.UserId,
                    "Asset Allocated",
                    $"New {asset.AssetName} ({asset.AssetCode}) " +
                    "has been assigned to you.",
                    "Success",
                    "ITAsset",
                    asset.AssetId);
            }

            // ============================================================
            // 5. NOTIFY ITS MANAGER
            // ============================================================

            await _notificationPublisher.PublishNotificationToRoleAsync(
                "ITS_MANAGER",
                "Asset Assigned",
                $"Asset {asset.AssetCode} allocated to " +
                $"{employee.FullName}.",
                "Info",
                "ITAsset",
                asset.AssetId);

            // ============================================================
            // 6. AUDIT LOG
            // ============================================================

            await _auditService.LogAsync(
                "Asset Assigned",
                "ITAsset",
                asset.AssetId.ToString(),
                new
                {
                    Status = AssetStatus.Available.ToString()
                },
                new
                {
                    Status = AssetStatus.Assigned.ToString(),
                    AssignedTo = employee.FullName,
                    asset.AssetCode
                });

            // ============================================================
            // 7. RETURN RESULT
            // ============================================================

            return Result<AssetDto>.Success(
                new AssetDto
                {
                    AssetId = asset.AssetId,
                    AssetCode = asset.AssetCode,
                    AssetName = asset.AssetName,
                    Status = asset.Status,
                    Condition = asset.Condition,
                    CurrentEmployeeId = employee.EmployeeId,
                    AssignedEmployeeCode = employee.EmployeeCode,
                    AssignedEmployeeName = employee.FullName
                },
                "Asset assigned successfully.");
        }
        catch
        {
            // ExecuteInTransactionAsync() already performs rollback.
            throw;
        }
    }

    public async Task<Result<AssetDto>> ReturnAssetAsync(long assetId, ReturnAssetDto dto)
    {
        var asset = await _uow.Assets.GetByIdAsync(assetId);
        if (asset == null)
        {
            return Result<AssetDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var activeAlloc = await _uow.AssetAllocations.FirstOrDefaultAsync(a => a.AssetId == assetId && a.AllocationStatus == AllocationStatus.Active);
        if (activeAlloc == null)
        {
            return Result<AssetDto>.Failure("No active allocation found for this asset.", "NOT_ALLOCATED");
        }

        var employee = await _uow.Employees.GetByIdAsync(activeAlloc.EmployeeId);

        await _uow.BeginTransactionAsync();
        try
        {
            // Close allocation
            activeAlloc.AllocationStatus = AllocationStatus.Returned;
            activeAlloc.ReturnDate = dto.ReturnDate;
            activeAlloc.ConditionAtReturn = dto.ConditionAtReturn;
            activeAlloc.Remarks = dto.Remarks;
            activeAlloc.UpdatedAt = DateTime.UtcNow;
            _uow.AssetAllocations.Update(activeAlloc);

            // Determine resulting asset status
            var targetStatus = dto.ResultingStatus ?? (dto.ConditionAtReturn switch
            {
                AssetCondition.Damaged => AssetStatus.Damaged,
                AssetCondition.NotWorking => AssetStatus.UnderMaintenance,
                _ => AssetStatus.Available
            });

            AssetStateMachine.ValidateTransition(asset.Status, targetStatus);

            asset.Status = targetStatus;
            asset.Condition = dto.ConditionAtReturn;
            asset.CurrentEmployeeId = null;
            asset.UpdatedAt = DateTime.UtcNow;
            _uow.Assets.Update(asset);

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();

            // Notify Manager and Employee
            var empUser = employee != null ? await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId) : null;
            if (empUser != null)
            {
                await _notificationPublisher.PublishNotificationAsync(
                    empUser.UserId,
                    "Asset Returned",
                    $"Asset {asset.AssetCode} ({asset.AssetName}) return has been processed.",
                    "Info",
                    "ITAsset",
                    asset.AssetId);
            }

            await _notificationPublisher.PublishNotificationToRoleAsync(
                "ITS_MANAGER",
                "Asset Returned",
                $"Asset {asset.AssetCode} returned by {employee?.FullName ?? "Employee"}. Status is now {targetStatus}.",
                "Info",
                "ITAsset",
                asset.AssetId);

            await _auditService.LogAsync(
                "Asset Returned",
                "ITAsset",
                asset.AssetId.ToString(),
                new { Status = AssetStatus.Assigned.ToString(), Employee = employee?.FullName },
                new { Status = targetStatus.ToString(), Condition = dto.ConditionAtReturn.ToString() });

            return Result<AssetDto>.Success(new AssetDto
            {
                AssetId = asset.AssetId,
                AssetCode = asset.AssetCode,
                AssetName = asset.AssetName,
                Status = asset.Status,
                Condition = asset.Condition
            }, "Asset returned successfully.");
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result<AssetDto>> TransferAssetAsync(long assetId, TransferAssetDto dto)
    {
        var asset = await _uow.Assets.GetByIdAsync(assetId);
        if (asset == null)
        {
            return Result<AssetDto>.Failure("Asset not found.", "NOT_FOUND");
        }

        var activeAlloc = await _uow.AssetAllocations.FirstOrDefaultAsync(a => a.AssetId == assetId && a.AllocationStatus == AllocationStatus.Active);
        if (activeAlloc == null)
        {
            return Result<AssetDto>.Failure("Asset is not currently allocated to anyone.", "NOT_ALLOCATED");
        }

        var currentEmp = await _uow.Employees.GetByIdAsync(activeAlloc.EmployeeId);
        var targetEmp = await _uow.Employees.GetByIdAsync(dto.TargetEmployeeId);
        if (targetEmp == null || !targetEmp.IsActive)
        {
            return Result<AssetDto>.Failure("Target employee not found or inactive.", "INVALID_TARGET_EMPLOYEE");
        }

        var userId = _currentUserService.UserId ?? 1;

        await _uow.BeginTransactionAsync();
        try
        {
            // 1. Close current allocation as Transferred
            activeAlloc.AllocationStatus = AllocationStatus.Transferred;
            activeAlloc.ReturnDate = DateTime.UtcNow;
            activeAlloc.Remarks = $"Transferred to {targetEmp.FullName} ({targetEmp.EmployeeCode}). {dto.Remarks}".Trim();
            activeAlloc.UpdatedAt = DateTime.UtcNow;
            _uow.AssetAllocations.Update(activeAlloc);

            // 2. Create new active allocation for target employee
            var newAlloc = new AssetAllocation
            {
                AssetId = asset.AssetId,
                EmployeeId = targetEmp.EmployeeId,
                AllocatedBy = userId,
                AllocationDate = DateTime.UtcNow,
                ExpectedReturnDate = dto.ExpectedReturnDate,
                AllocationStatus = AllocationStatus.Active,
                ConditionAtAllocation = dto.Condition,
                Remarks = $"Transferred from {currentEmp?.FullName} ({currentEmp?.EmployeeCode}). {dto.Remarks}".Trim()
            };
            await _uow.AssetAllocations.AddAsync(newAlloc);

            // 3. Update asset
            asset.CurrentEmployeeId = targetEmp.EmployeeId;
            asset.Condition = dto.Condition;
            asset.UpdatedAt = DateTime.UtcNow;
            _uow.Assets.Update(asset);

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();

            // Notify both employees
            var curUser = currentEmp != null ? await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == currentEmp.EmployeeId) : null;
            if (curUser != null)
            {
                await _notificationPublisher.PublishNotificationAsync(
                    curUser.UserId,
                    "Asset Transferred",
                    $"Asset {asset.AssetCode} has been transferred to {targetEmp.FullName}.",
                    "Info",
                    "ITAsset",
                    asset.AssetId);
            }

            var targetUser = await _uow.Users.FirstOrDefaultAsync(u => u.EmployeeId == targetEmp.EmployeeId);
            if (targetUser != null)
            {
                await _notificationPublisher.PublishNotificationAsync(
                    targetUser.UserId,
                    "Asset Transferred To You",
                    $"Asset {asset.AssetCode} ({asset.AssetName}) has been transferred to you from {currentEmp?.FullName}.",
                    "Success",
                    "ITAsset",
                    asset.AssetId);
            }

            await _auditService.LogAsync(
                "Asset Transferred",
                "ITAsset",
                asset.AssetId.ToString(),
                new { FromEmployee = currentEmp?.FullName },
                new { ToEmployee = targetEmp.FullName });

            return Result<AssetDto>.Success(new AssetDto
            {
                AssetId = asset.AssetId,
                AssetCode = asset.AssetCode,
                AssetName = asset.AssetName,
                Status = asset.Status,
                Condition = asset.Condition,
                CurrentEmployeeId = targetEmp.EmployeeId,
                AssignedEmployeeCode = targetEmp.EmployeeCode,
                AssignedEmployeeName = targetEmp.FullName
            }, "Asset transferred successfully.");
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result<List<AllocationHistoryDto>>> GetAllocationHistoryAsync(long assetId)
    {
        var allocations = (await _uow.AssetAllocations.FindAsync(a => a.AssetId == assetId))
            .OrderByDescending(a => a.AllocationDate)
            .ToList();

        var asset = await _uow.Assets.GetByIdAsync(assetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var departments = (await _uow.Departments.GetAllAsync()).ToDictionary(d => d.DepartmentId, d => d.Name);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);

        var dtos = allocations.Select(a =>
        {
            employees.TryGetValue(a.EmployeeId, out var emp);
            string? deptName = null;
            if (emp?.DepartmentId.HasValue == true && departments.TryGetValue(emp.DepartmentId.Value, out var dname))
            {
                deptName = dname;
            }

            users.TryGetValue(a.AllocatedBy, out var allocUser);

            return new AllocationHistoryDto
            {
                AllocationId = a.AllocationId,
                AssetId = a.AssetId,
                AssetCode = asset?.AssetCode ?? "",
                AssetName = asset?.AssetName ?? "",
                EmployeeId = a.EmployeeId,
                EmployeeCode = emp?.EmployeeCode ?? "",
                EmployeeName = emp?.FullName ?? "Unknown",
                DepartmentName = deptName,
                AllocationDate = a.AllocationDate,
                ExpectedReturnDate = a.ExpectedReturnDate,
                ReturnDate = a.ReturnDate,
                AllocationStatus = a.AllocationStatus,
                ConditionAtAllocation = a.ConditionAtAllocation,
                ConditionAtReturn = a.ConditionAtReturn,
                Remarks = a.Remarks,
                AllocatedByName = allocUser?.Username ?? "System"
            };
        }).ToList();

        return Result<List<AllocationHistoryDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<AllocationHistoryDto>>> GetAllAllocationsAsync(QueryParameters parameters)
    {
        var allAllocations = await _uow.AssetAllocations.GetAllAsync();
        var assets = (await _uow.Assets.GetAllAsync()).ToDictionary(a => a.AssetId);
        var employees = (await _uow.Employees.GetAllAsync()).ToDictionary(e => e.EmployeeId);
        var departments = (await _uow.Departments.GetAllAsync()).ToDictionary(d => d.DepartmentId, d => d.Name);
        var users = (await _uow.Users.GetAllAsync()).ToDictionary(u => u.UserId);

        var query = allAllocations.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(a => (assets.TryGetValue(a.AssetId, out var ast) && (ast.AssetCode.ToLower().Contains(search) || ast.AssetName.ToLower().Contains(search))) ||
                                     (employees.TryGetValue(a.EmployeeId, out var emp) && (emp.EmployeeCode.ToLower().Contains(search) || emp.FullName.ToLower().Contains(search))));
        }

        var list = query.ToList();
        var total = list.Count;

        var items = list
            .OrderByDescending(a => a.AllocationDate)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(a =>
            {
                assets.TryGetValue(a.AssetId, out var asset);
                employees.TryGetValue(a.EmployeeId, out var emp);
                string? deptName = null;
                if (emp?.DepartmentId.HasValue == true && departments.TryGetValue(emp.DepartmentId.Value, out var dname))
                {
                    deptName = dname;
                }
                users.TryGetValue(a.AllocatedBy, out var allocUser);

                return new AllocationHistoryDto
                {
                    AllocationId = a.AllocationId,
                    AssetId = a.AssetId,
                    AssetCode = asset?.AssetCode ?? "",
                    AssetName = asset?.AssetName ?? "",
                    EmployeeId = a.EmployeeId,
                    EmployeeCode = emp?.EmployeeCode ?? "",
                    EmployeeName = emp?.FullName ?? "Unknown",
                    DepartmentName = deptName,
                    AllocationDate = a.AllocationDate,
                    ExpectedReturnDate = a.ExpectedReturnDate,
                    ReturnDate = a.ReturnDate,
                    AllocationStatus = a.AllocationStatus,
                    ConditionAtAllocation = a.ConditionAtAllocation,
                    ConditionAtReturn = a.ConditionAtReturn,
                    Remarks = a.Remarks,
                    AllocatedByName = allocUser?.Username ?? "System"
                };
            }).ToList();

        return Result<PagedResult<AllocationHistoryDto>>.Success(new PagedResult<AllocationHistoryDto>(items, total, parameters.PageNumber, parameters.PageSize));
    }
}
