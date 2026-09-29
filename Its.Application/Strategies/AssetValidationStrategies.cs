using Its.Domain.Entities;
using Its.Domain.Enums;

namespace Its.Application.Strategies;

public class CheckResult
{
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
}

public class AssetCheckResult
{
    public long AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public List<CheckResult> Checks { get; set; } = new();
    public string OverallSummary { get; set; } = string.Empty;
}

public interface IAssetValidationRule
{
    string Name { get; }
    CheckResult Validate(ITAsset asset, Employee? employee, int minWarrantyDays = 30);
}

public class AvailabilityRule : IAssetValidationRule
{
    public string Name => "Asset Availability";

    public CheckResult Validate(ITAsset asset, Employee? employee, int minWarrantyDays = 30)
    {
        if (asset.Status != AssetStatus.Available)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = $"Asset {asset.AssetCode} is not available (Current Status: {asset.Status}).",
                SuggestedAction = "Select an alternate available asset from inventory."
            };
        }

        return new CheckResult
        {
            RuleName = Name,
            Passed = true,
            Message = "Asset is currently in stock and Available."
        };
    }
}

public class ConditionRule : IAssetValidationRule
{
    public string Name => "Working Condition";

    public CheckResult Validate(ITAsset asset, Employee? employee, int minWarrantyDays = 30)
    {
        if (asset.Condition == AssetCondition.Damaged || asset.Condition == AssetCondition.NotWorking)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = $"Asset {asset.AssetCode} condition is {asset.Condition}.",
                SuggestedAction = "Mark asset as Under Maintenance and send for repair."
            };
        }

        return new CheckResult
        {
            RuleName = Name,
            Passed = true,
            Message = $"Asset is in working condition ({asset.Condition})."
        };
    }
}

public class WarrantyRule : IAssetValidationRule
{
    public string Name => "Warranty & Expiry";

    public CheckResult Validate(ITAsset asset, Employee? employee, int minWarrantyDays = 30)
    {
        if (!asset.WarrantyExpiryDate.HasValue)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = true,
                Message = "No explicit warranty expiry registered; verified usable."
            };
        }

        var today = DateTime.UtcNow.Date;
        var expiry = asset.WarrantyExpiryDate.Value.Date;

        if (expiry < today)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = $"Asset warranty expired on {expiry:yyyy-MM-dd}.",
                SuggestedAction = "Mark as Not Suitable (expired warranty)."
            };
        }

        var remainingDays = (expiry - today).TotalDays;
        if (remainingDays < minWarrantyDays)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = $"Asset warranty is near expiry ({remainingDays:F0} days remaining, minimum required is {minWarrantyDays} days).",
                SuggestedAction = "Mark as Not Suitable (near expiry); choose an asset with longer warranty."
            };
        }

        return new CheckResult
        {
            RuleName = Name,
            Passed = true,
            Message = $"Warranty valid until {expiry:yyyy-MM-dd} ({remainingDays:F0} days remaining)."
        };
    }
}

public class EmployeeActiveRule : IAssetValidationRule
{
    public string Name => "Employee Eligibility";

    public CheckResult Validate(ITAsset asset, Employee? employee, int minWarrantyDays = 30)
    {
        if (employee == null)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = "Target employee not found.",
                SuggestedAction = "Provide a valid active employee."
            };
        }

        if (!employee.IsActive)
        {
            return new CheckResult
            {
                RuleName = Name,
                Passed = false,
                Message = $"Employee {employee.FullName} ({employee.EmployeeCode}) is inactive.",
                SuggestedAction = "Cannot allocate asset to an inactive employee."
            };
        }

        return new CheckResult
        {
            RuleName = Name,
            Passed = true,
            Message = $"Employee {employee.FullName} ({employee.EmployeeCode}) is active and eligible."
        };
    }
}

public class AssetValidationPipeline
{
    private readonly List<IAssetValidationRule> _rules;

    public AssetValidationPipeline(IEnumerable<IAssetValidationRule>? customRules = null)
    {
        _rules = customRules?.ToList() ?? new List<IAssetValidationRule>
        {
            new AvailabilityRule(),
            new ConditionRule(),
            new WarrantyRule(),
            new EmployeeActiveRule()
        };
    }

    public AssetCheckResult Execute(ITAsset asset, Employee? employee, int minWarrantyDays = 30)
    {
        var result = new AssetCheckResult
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName
        };

        bool allPassed = true;
        foreach (var rule in _rules)
        {
            var check = rule.Validate(asset, employee, minWarrantyDays);
            result.Checks.Add(check);
            if (!check.Passed)
            {
                allPassed = false;
            }
        }

        result.IsEligible = allPassed;
        result.OverallSummary = allPassed
            ? "All validation checks passed successfully. Asset is ready for allocation."
            : "Asset failed one or more validation checks and cannot be allocated directly.";

        return result;
    }
}
