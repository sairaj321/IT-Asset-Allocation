using FluentAssertions;
using Its.Application.Strategies;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Domain.Exceptions;
using Its.Domain.State;
using Xunit;

namespace Its.Tests;

public class AssetValidationTests
{
    private readonly AvailabilityRule _availabilityRule = new();
    private readonly ConditionRule _conditionRule = new();
    private readonly WarrantyRule _warrantyRule = new();
    private readonly EmployeeActiveRule _employeeRule = new();
    private readonly AssetValidationPipeline _pipeline = new();

    [Fact]
    public void AvailabilityRule_ShouldPass_WhenAssetIsAvailable()
    {
        var asset = new ITAsset { AssetCode = "LAP-001", Status = AssetStatus.Available };
        var result = _availabilityRule.Validate(asset, null);

        result.Passed.Should().BeTrue();
    }

    [Theory]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.UnderMaintenance)]
    [InlineData(AssetStatus.InRepair)]
    [InlineData(AssetStatus.Damaged)]
    [InlineData(AssetStatus.Lost)]
    [InlineData(AssetStatus.Retired)]
    public void AvailabilityRule_ShouldFail_WhenAssetIsNotAvailable(AssetStatus status)
    {
        var asset = new ITAsset { AssetCode = "LAP-001", Status = status };
        var result = _availabilityRule.Validate(asset, null);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain(status.ToString());
    }

    [Theory]
    [InlineData(AssetCondition.Good)]
    [InlineData(AssetCondition.Fair)]
    public void ConditionRule_ShouldPass_WhenConditionIsGoodOrFair(AssetCondition condition)
    {
        var asset = new ITAsset { AssetCode = "LAP-001", Condition = condition };
        var result = _conditionRule.Validate(asset, null);

        result.Passed.Should().BeTrue();
    }

    [Theory]
    [InlineData(AssetCondition.Damaged)]
    [InlineData(AssetCondition.NotWorking)]
    public void ConditionRule_ShouldFail_WhenConditionIsDamagedOrNotWorking(AssetCondition condition)
    {
        var asset = new ITAsset { AssetCode = "LAP-001", Condition = condition };
        var result = _conditionRule.Validate(asset, null);

        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void WarrantyRule_ShouldFail_WhenWarrantyIsExpired()
    {
        var asset = new ITAsset
        {
            AssetCode = "LAP-001",
            WarrantyExpiryDate = DateTime.UtcNow.AddDays(-10)
        };
        var result = _warrantyRule.Validate(asset, null, minWarrantyDays: 30);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public void WarrantyRule_ShouldFail_WhenWarrantyIsNearExpiry()
    {
        var asset = new ITAsset
        {
            AssetCode = "LAP-001",
            WarrantyExpiryDate = DateTime.UtcNow.AddDays(15) // Less than 30 days
        };
        var result = _warrantyRule.Validate(asset, null, minWarrantyDays: 30);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("near expiry");
    }

    [Fact]
    public void WarrantyRule_ShouldPass_WhenWarrantyIsValidLongTerm()
    {
        var asset = new ITAsset
        {
            AssetCode = "LAP-001",
            WarrantyExpiryDate = DateTime.UtcNow.AddMonths(12)
        };
        var result = _warrantyRule.Validate(asset, null, minWarrantyDays: 30);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void EmployeeRule_ShouldFail_WhenEmployeeIsInactive()
    {
        var employee = new Employee { EmployeeCode = "EMP001", FirstName = "John", LastName = "Doe", IsActive = false };
        var result = _employeeRule.Validate(new ITAsset(), employee);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("inactive");
    }

    [Fact]
    public void Pipeline_ShouldReturnEligible_WhenAllChecksPass()
    {
        var asset = new ITAsset
        {
            AssetId = 1,
            AssetCode = "LAP-0002",
            AssetName = "HP ProBook 450",
            Status = AssetStatus.Available,
            Condition = AssetCondition.Good,
            WarrantyExpiryDate = DateTime.UtcNow.AddMonths(18)
        };

        var emp = new Employee { EmployeeId = 1, EmployeeCode = "EMP001", FirstName = "Sairaj", LastName = "Kadlag", IsActive = true };

        var result = _pipeline.Execute(asset, emp);
        result.IsEligible.Should().BeTrue();
        result.Checks.Should().HaveCount(4);
        result.Checks.All(c => c.Passed).Should().BeTrue();
    }
}

public class StateMachineTests
{
    [Fact]
    public void RequestStateMachine_ValidTransitions_ShouldPass()
    {
        RequestStateMachine.CanTransition(RequestStatus.Submitted, RequestStatus.UnderReview).Should().BeTrue();
        RequestStateMachine.CanTransition(RequestStatus.Submitted, RequestStatus.AssignedToExecutive).Should().BeTrue();
        RequestStateMachine.CanTransition(RequestStatus.AssignedToExecutive, RequestStatus.Checking).Should().BeTrue();
        RequestStateMachine.CanTransition(RequestStatus.Checking, RequestStatus.Allocated).Should().BeTrue();
        RequestStateMachine.CanTransition(RequestStatus.Checking, RequestStatus.Rejected).Should().BeTrue();
    }

    [Fact]
    public void RequestStateMachine_InvalidTransition_ShouldThrowConflictException()
    {
        var act = () => RequestStateMachine.ValidateTransition(RequestStatus.Allocated, RequestStatus.Submitted);
        act.Should().Throw<ConflictException>()
            .WithMessage("*Illegal request status transition*");
    }

    [Fact]
    public void AssetStateMachine_ValidTransitions_ShouldPass()
    {
        AssetStateMachine.CanTransition(AssetStatus.Available, AssetStatus.Assigned).Should().BeTrue();
        AssetStateMachine.CanTransition(AssetStatus.Assigned, AssetStatus.Available).Should().BeTrue();
        AssetStateMachine.CanTransition(AssetStatus.Assigned, AssetStatus.UnderMaintenance).Should().BeTrue();
        AssetStateMachine.CanTransition(AssetStatus.UnderMaintenance, AssetStatus.InRepair).Should().BeTrue();
        AssetStateMachine.CanTransition(AssetStatus.InRepair, AssetStatus.Available).Should().BeTrue();
    }

    [Fact]
    public void AssetStateMachine_InvalidTransition_ShouldThrowConflictException()
    {
        var act = () => AssetStateMachine.ValidateTransition(AssetStatus.Retired, AssetStatus.Assigned);
        act.Should().Throw<ConflictException>()
            .WithMessage("*Illegal asset status transition*");
    }
}
