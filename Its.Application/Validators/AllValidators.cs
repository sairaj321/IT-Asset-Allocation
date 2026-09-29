using FluentValidation;
using Its.Application.DTOs;

namespace Its.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username or employee code is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}

public class CreateAssetRequestDtoValidator : AbstractValidator<CreateAssetRequestDto>
{
    public CreateAssetRequestDtoValidator()
    {
        RuleFor(x => x.AssetCategoryId).GreaterThan(0).WithMessage("Please select a valid asset category.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Reason is required and cannot exceed 500 characters.");
    }
}

public class CreateAssetDtoValidator : AbstractValidator<CreateAssetDto>
{
    public CreateAssetDtoValidator()
    {
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(30).WithMessage("Asset code is required.");
        RuleFor(x => x.AssetName).NotEmpty().MaximumLength(150).WithMessage("Asset name is required.");
        RuleFor(x => x.AssetCategoryId).GreaterThan(0).WithMessage("Valid category is required.");
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100).WithMessage("Serial number is required.");
        RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0).WithMessage("Purchase cost cannot be negative.");
    }
}

public class AssignAssetDtoValidator : AbstractValidator<AssignAssetDto>
{
    public AssignAssetDtoValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("Valid employee is required.");
    }
}

public class ReturnAssetDtoValidator : AbstractValidator<ReturnAssetDto>
{
    public ReturnAssetDtoValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class TransferAssetDtoValidator : AbstractValidator<TransferAssetDto>
{
    public TransferAssetDtoValidator()
    {
        RuleFor(x => x.TargetEmployeeId).GreaterThan(0).WithMessage("Target employee is required.");
    }
}

public class CreateExecutiveDtoValidator : AbstractValidator<CreateExecutiveDto>
{
    public CreateExecutiveDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(80);
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(6);
    }
}

public class CreateRepairDtoValidator : AbstractValidator<CreateRepairDto>
{
    public CreateRepairDtoValidator()
    {
        RuleFor(x => x.AssetId).GreaterThan(0).WithMessage("Valid asset is required.");
        RuleFor(x => x.IssueDescription).NotEmpty().MaximumLength(1000).WithMessage("Issue description is required.");
    }
}

public class CreateReplacementDtoValidator : AbstractValidator<CreateReplacementDto>
{
    public CreateReplacementDtoValidator()
    {
        RuleFor(x => x.OldAssetId).GreaterThan(0).WithMessage("Valid existing asset is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Reason for replacement is required.");
    }
}
