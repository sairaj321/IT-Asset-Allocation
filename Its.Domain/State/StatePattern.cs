using Its.Domain.Enums;
using Its.Domain.Exceptions;

namespace Its.Domain.State;

public interface IRequestState
{
    RequestStatus Status { get; }
    bool CanTransitionTo(RequestStatus nextStatus);
    void ValidateTransition(RequestStatus nextStatus);
}

public class RequestStateMachine
{
    private static readonly Dictionary<RequestStatus, HashSet<RequestStatus>> AllowedTransitions = new()
    {
        { RequestStatus.Submitted, new() { RequestStatus.UnderReview, RequestStatus.AssignedToExecutive, RequestStatus.Rejected, RequestStatus.ReturnedForCorrection, RequestStatus.Cancelled } },
        { RequestStatus.UnderReview, new() { RequestStatus.AssignedToExecutive, RequestStatus.Rejected, RequestStatus.ReturnedForCorrection, RequestStatus.Cancelled } },
        { RequestStatus.ReturnedForCorrection, new() { RequestStatus.Submitted, RequestStatus.Cancelled } },
        { RequestStatus.AssignedToExecutive, new() { RequestStatus.Checking, RequestStatus.Allocated, RequestStatus.NotAvailable, RequestStatus.Rejected, RequestStatus.Cancelled } },
        { RequestStatus.Checking, new() { RequestStatus.Allocated, RequestStatus.NotAvailable, RequestStatus.Rejected, RequestStatus.Cancelled } },
        { RequestStatus.NotAvailable, new() { RequestStatus.Checking, RequestStatus.Rejected, RequestStatus.Cancelled } },
        { RequestStatus.Allocated, new() { /* Terminal */ } },
        { RequestStatus.Rejected, new() { /* Terminal */ } },
        { RequestStatus.Cancelled, new() { /* Terminal */ } }
    };

    public static bool CanTransition(RequestStatus current, RequestStatus target)
    {
        return AllowedTransitions.TryGetValue(current, out var targets) && targets.Contains(target);
    }

    public static void ValidateTransition(RequestStatus current, RequestStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new ConflictException($"Illegal request status transition from '{current}' to '{target}'.");
        }
    }
}

public class AssetStateMachine
{
    private static readonly Dictionary<AssetStatus, HashSet<AssetStatus>> AllowedTransitions = new()
    {
        { AssetStatus.Available, new() { AssetStatus.Assigned, AssetStatus.UnderMaintenance, AssetStatus.Lost, AssetStatus.Retired } },
        { AssetStatus.Assigned, new() { AssetStatus.Available, AssetStatus.UnderMaintenance, AssetStatus.Damaged, AssetStatus.Replaced, AssetStatus.Lost } },
        { AssetStatus.UnderMaintenance, new() { AssetStatus.InRepair, AssetStatus.Available, AssetStatus.Damaged, AssetStatus.Retired, AssetStatus.Lost } },
        { AssetStatus.InRepair, new() { AssetStatus.Available, AssetStatus.Damaged, AssetStatus.Retired, AssetStatus.Lost } },
        { AssetStatus.Damaged, new() { AssetStatus.UnderMaintenance, AssetStatus.InRepair, AssetStatus.Retired, AssetStatus.Lost } },
        { AssetStatus.Replaced, new() { AssetStatus.Retired, AssetStatus.UnderMaintenance, AssetStatus.Available } },
        { AssetStatus.Lost, new() { AssetStatus.Available, AssetStatus.UnderMaintenance } }, // Manager can reactivate if found
        { AssetStatus.Retired, new() { /* Terminal */ } }
    };

    public static bool CanTransition(AssetStatus current, AssetStatus target)
    {
        return AllowedTransitions.TryGetValue(current, out var targets) && targets.Contains(target);
    }

    public static void ValidateTransition(AssetStatus current, AssetStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new ConflictException($"Illegal asset status transition from '{current}' to '{target}'.");
        }
    }
}
