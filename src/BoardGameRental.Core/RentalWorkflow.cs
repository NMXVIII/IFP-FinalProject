namespace BoardGameRental.Core;

public abstract class RentalState
{
    private RentalState() { }

    public sealed class Draft : RentalState
    {
        public RentalDraft Request { get; }
        public Draft(RentalDraft request) { Request = request; }
    }

    public sealed class Ready : RentalState
    {
        public ValidatedRental Rental { get; }
        internal Ready(ValidatedRental rental) { Rental = rental; }
    }

    public sealed class Confirmed : RentalState
    {
        public ValidatedRental Rental { get; }
        internal Confirmed(ValidatedRental rental) { Rental = rental; }
    }
}

public sealed class WorkflowResult
{
    public RentalState State { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    internal WorkflowResult(RentalState state, string? error)
    {
        State = state;
        Error = error;
    }
}

public static class RentalWorkflow
{
    public static WorkflowResult Prepare(RentalState state) => state switch
    {
        RentalState.Draft draft => PrepareDraft(draft),
        RentalState.Ready => new(state, "Rental is already prepared."),
        RentalState.Confirmed => new(state, "A confirmed rental cannot be prepared again."),
        _ => throw new ArgumentException("Unknown rental state.", nameof(state))
    };

    private static WorkflowResult PrepareDraft(RentalState.Draft state)
    {
        if (!ValidatedRental.TryCreate(state.Request, out var rental, out var errors))
            return new(state, string.Join("; ", errors));
        if (rental is null)
            return new(state, "Factory did not return a validated rental.");
        return new(new RentalState.Ready(rental), null);
    }

    public static WorkflowResult Confirm(RentalState state) => state switch
    {
        RentalState.Draft => new(state, "Prepare the rental before confirmation."),
        RentalState.Ready ready => new(new RentalState.Confirmed(ready.Rental), null),
        RentalState.Confirmed => new(state, null),
        _ => throw new ArgumentException("Unknown rental state.", nameof(state))
    };

    public static string Describe(RentalState state) => state switch
    {
        RentalState.Draft => "Draft",
        RentalState.Ready => "Ready",
        RentalState.Confirmed => "Confirmed",
        _ => throw new ArgumentException("Unknown rental state.", nameof(state))
    };
}
