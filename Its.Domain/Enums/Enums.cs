namespace Its.Domain.Enums;

public enum AssetStatus
{
    Available,
    Assigned,
    UnderMaintenance,
    InRepair,
    Damaged,
    Lost,
    Replaced,
    Retired
}

public enum AssetCondition
{
    Good,
    Fair,
    Damaged,
    NotWorking
}

public enum RequestStatus
{
    Submitted,
    UnderReview,
    ReturnedForCorrection,
    AssignedToExecutive,
    Checking,
    NotAvailable,
    Allocated,
    Rejected,
    Cancelled
}

public enum AllocationStatus
{
    Active,
    Returned,
    Transferred,
    Cancelled
}

public enum RepairStatus
{
    Reported,
    UnderReview,
    InRepair,
    Completed,
    ClosedNoRepair
}

public enum ReplacementStatus
{
    Requested,
    ConditionChecked,
    Approved,
    Rejected,
    Completed
}

public enum Priority
{
    Low,
    Normal,
    High
}

public enum RequestType
{
    NewAsset,
    Replacement,
    Return,
    Repair
}
