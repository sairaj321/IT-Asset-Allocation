using Its.Api.Services;
using Its.Application.Common;
using Its.Application.DTOs;
using Its.Application.Interfaces;
using Its.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Its.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Data);
        }

        return result.Code switch
        {
            "NOT_FOUND" => NotFound(new { code = result.Code, message = result.Message }),
            "UNAUTHORIZED" => Unauthorized(new { code = result.Code, message = result.Message }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { code = result.Code, message = result.Message }),
            "CONFLICT" => Conflict(new { code = result.Code, message = result.Message }),
            "ACCOUNT_LOCKED" => StatusCode(StatusCodes.Status423Locked, new { code = result.Code, message = result.Message }),
            _ => BadRequest(new { code = result.Code, message = result.Message, errors = result.Errors })
        };
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.Succeeded)
        {
            return Ok(new { message = result.Message ?? "Operation completed successfully." });
        }

        return result.Code switch
        {
            "NOT_FOUND" => NotFound(new { code = result.Code, message = result.Message }),
            "UNAUTHORIZED" => Unauthorized(new { code = result.Code, message = result.Message }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { code = result.Code, message = result.Message }),
            "CONFLICT" => Conflict(new { code = result.Code, message = result.Message }),
            _ => BadRequest(new { code = result.Code, message = result.Message, errors = result.Errors })
        };
    }
}

#region Auth Controller
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return HandleResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync();
        return HandleResult(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(request);
        return HandleResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _authService.GetCurrentUserAsync();
        return HandleResult(result);
    }
}
#endregion

#region Dashboard Controller
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("employee")]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        var result = await _dashboardService.GetEmployeeDashboardAsync();
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpGet("manager")]
    public async Task<IActionResult> GetManagerDashboard()
    {
        var result = await _dashboardService.GetManagerDashboardAsync();
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_EXECUTIVE,ITS_MANAGER")]
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutiveDashboard()
    {
        var result = await _dashboardService.GetExecutiveDashboardAsync();
        return HandleResult(result);
    }
}
#endregion

#region Employees Controller
[Authorize]
[Route("api/employees")]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] QueryParameters parameters)
    {
        var result = await _employeeService.GetEmployeesAsync(parameters);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployeeById(long id)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("{id}/assets")]
    public async Task<IActionResult> GetEmployeeAssets(long id)
    {
        var result = await _employeeService.GetEmployeeAssetsAsync(id);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        var result = await _employeeService.CreateEmployeeAsync(dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(long id, [FromBody] UpdateEmployeeDto dto)
    {
        var result = await _employeeService.UpdateEmployeeAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region ITS Executives Controller
[Authorize(Roles = "ITS_MANAGER")]
[Route("api/its-executives")]
public class ItsExecutivesController : ApiControllerBase
{
    private readonly IExecutiveService _executiveService;

    public ItsExecutivesController(IExecutiveService executiveService)
    {
        _executiveService = executiveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetExecutives([FromQuery] QueryParameters parameters)
    {
        var result = await _executiveService.GetExecutivesAsync(parameters);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetExecutiveById(long id)
    {
        var result = await _executiveService.GetExecutiveByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExecutive([FromBody] CreateExecutiveDto dto)
    {
        var result = await _executiveService.CreateExecutiveAsync(dto);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExecutive(long id, [FromBody] UpdateExecutiveDto dto)
    {
        var result = await _executiveService.UpdateExecutiveAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto)
    {
        var result = await _executiveService.ChangeExecutiveStatusAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region Asset Categories Controller
[Authorize]
[Route("api/asset-categories")]
public class AssetCategoriesController : ApiControllerBase
{
    private readonly IAssetService _assetService;

    public AssetCategoriesController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _assetService.GetCategoriesAsync();
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] AssetCategoryDto dto)
    {
        var result = await _assetService.CreateCategoryAsync(dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] AssetCategoryDto dto)
    {
        var result = await _assetService.UpdateCategoryAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region Assets Controller
[Authorize]
[Route("api/assets")]
public class AssetsController : ApiControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IAllocationService _allocationService;

    public AssetsController(IAssetService assetService, IAllocationService allocationService)
    {
        _assetService = assetService;
        _allocationService = allocationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssets([FromQuery] AssetFilterDto filter)
    {
        var result = await _assetService.GetAssetsAsync(filter);
        return HandleResult(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableAssets([FromQuery] int? categoryId)
    {
        var result = await _assetService.GetAvailableAssetsAsync(categoryId);
        return HandleResult(result);
    }

    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllAllocations([FromQuery] QueryParameters parameters)
    {
        var result = await _allocationService.GetAllAllocationsAsync(parameters);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssetById(long id)
    {
        var result = await _assetService.GetAssetByIdAsync(id);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpPost]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetDto dto)
    {
        var result = await _assetService.CreateAssetAsync(dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsset(long id, [FromBody] UpdateAssetDto dto)
    {
        var result = await _assetService.UpdateAssetAsync(id, dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateAsset(long id)
    {
        var result = await _assetService.DeactivateAssetAsync(id);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    [HttpPost("{id}/check")]
    public async Task<IActionResult> CheckAsset(long id, [FromQuery] long? employeeId)
    {
        var result = await _assetService.CheckAssetAsync(id, employeeId);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignAsset(long id, [FromBody] AssignAssetDto dto)
    {
        var result = await _allocationService.AssignAssetAsync(id, dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    [HttpPost("{id}/return")]
    public async Task<IActionResult> ReturnAsset(long id, [FromBody] ReturnAssetDto dto)
    {
        var result = await _allocationService.ReturnAssetAsync(id, dto);
        return HandleResult(result);
    }

    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> TransferAsset(long id, [FromBody] TransferAssetDto dto)
    {
        var result = await _allocationService.TransferAssetAsync(id, dto);
        return HandleResult(result);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetAllocationHistory(long id)
    {
        var result = await _allocationService.GetAllocationHistoryAsync(id);
        return HandleResult(result);
    }
}
#endregion

#region Requests Controller
[Authorize]
[Route("api/requests")]
public class RequestsController : ApiControllerBase
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    [Authorize(Roles = "ITS_MANAGER")]
    public async Task<IActionResult> GetAllRequests([FromQuery] QueryParameters parameters, [FromQuery] RequestStatus? status)
    {
        var result = await _requestService.GetAllRequestsAsync(parameters, status);
        return HandleResult(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests([FromQuery] QueryParameters parameters)
    {
        var result = await _requestService.GetMyRequestsAsync(parameters);
        return HandleResult(result);
    }

    [HttpGet("assigned")]
    [Authorize(Roles = "ITS_EXECUTIVE,ITS_MANAGER")]
    public async Task<IActionResult> GetAssignedRequests([FromQuery] QueryParameters parameters)
    {
        var result = await _requestService.GetAssignedRequestsAsync(parameters);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequestById(long id)
    {
        var result = await _requestService.GetRequestByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateAssetRequestDto dto)
    {
        var result = await _requestService.CreateRequestAsync(dto);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRequest(long id, [FromBody] UpdateAssetRequestDto dto)
    {
        var result = await _requestService.UpdateRequestAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/validate")]
    [Authorize(Roles = "ITS_MANAGER")]
    public async Task<IActionResult> ValidateRequest(long id, [FromBody] ValidateRequestDto dto)
    {
        var result = await _requestService.ValidateRequestAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/assign-executive")]
    [Authorize(Roles = "ITS_MANAGER")]
    public async Task<IActionResult> AssignExecutive(long id, [FromBody] AssignExecutiveDto dto)
    {
        var result = await _requestService.AssignExecutiveAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    public async Task<IActionResult> RejectRequest(long id, [FromBody] RejectRequestDto dto)
    {
        var result = await _requestService.RejectRequestAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/allocate")]
    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    public async Task<IActionResult> AllocateRequest(long id, [FromBody] AllocateRequestDto dto)
    {
        var result = await _requestService.AllocateRequestAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region Repairs Controller
[Authorize]
[Route("api/repairs")]
public class RepairsController : ApiControllerBase
{
    private readonly IRepairService _repairService;

    public RepairsController(IRepairService repairService)
    {
        _repairService = repairService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRepairs([FromQuery] QueryParameters parameters, [FromQuery] RepairStatus? status)
    {
        var result = await _repairService.GetRepairsAsync(parameters, status);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRepairById(long id)
    {
        var result = await _repairService.GetRepairByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRepair([FromBody] CreateRepairDto dto)
    {
        var result = await _repairService.CreateRepairAsync(dto);
        return HandleResult(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "ITS_MANAGER,ITS_EXECUTIVE")]
    public async Task<IActionResult> UpdateRepairStatus(long id, [FromBody] UpdateRepairStatusDto dto)
    {
        var result = await _repairService.UpdateRepairStatusAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region Replacements Controller
[Authorize]
[Route("api/replacements")]
public class ReplacementsController : ApiControllerBase
{
    private readonly IReplacementService _replacementService;

    public ReplacementsController(IReplacementService replacementService)
    {
        _replacementService = replacementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReplacements([FromQuery] QueryParameters parameters, [FromQuery] ReplacementStatus? status)
    {
        var result = await _replacementService.GetReplacementsAsync(parameters, status);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReplacementById(long id)
    {
        var result = await _replacementService.GetReplacementByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReplacement([FromBody] CreateReplacementDto dto)
    {
        var result = await _replacementService.CreateReplacementAsync(dto);
        return HandleResult(result);
    }

    [HttpPut("{id}/decision")]
    [Authorize(Roles = "ITS_MANAGER")]
    public async Task<IActionResult> ProcessDecision(long id, [FromBody] ReplacementDecisionDto dto)
    {
        var result = await _replacementService.ProcessReplacementAsync(id, dto);
        return HandleResult(result);
    }
}
#endregion

#region Notifications Controller
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var result = await _notificationService.GetNotificationsAsync();
        return HandleResult(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return HandleResult(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync();
        return HandleResult(result);
    }
}
#endregion

#region Reports Controller
[Authorize(Roles = "ITS_MANAGER")]
[Route("api/reports")]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("assets")]
    public async Task<IActionResult> GetAssetReport([FromQuery] string? format)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportService.GenerateAssetReportCsvAsync();
            return File(bytes, "text/csv", $"ITS_Assets_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        var data = await _reportService.GetAssetReportDataAsync();
        return Ok(data);
    }

    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocationReport([FromQuery] string? format)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportService.GenerateAllocationReportCsvAsync();
            return File(bytes, "text/csv", $"ITS_Allocations_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        var data = await _reportService.GetAllocationReportDataAsync();
        return Ok(data);
    }

    [HttpGet("repairs")]
    public async Task<IActionResult> GetRepairReport([FromQuery] string? format)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportService.GenerateRepairReportCsvAsync();
            return File(bytes, "text/csv", $"ITS_Repairs_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        var data = await _reportService.GetRepairReportDataAsync();
        return Ok(data);
    }
}
#endregion
