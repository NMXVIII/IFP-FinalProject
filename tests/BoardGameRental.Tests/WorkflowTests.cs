using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void WorkflowCases()
    {
        var draft = new RentalState.Draft(ValidDraft());
        var ready = RentalWorkflow.Prepare(draft);
        var confirmed = RentalWorkflow.Confirm(ready.State);
        Check(RentalWorkflow.Describe(draft) == "Draft", "Draft branch failed.");
        Check(ready.IsSuccess && ready.State is RentalState.Ready && RentalWorkflow.Describe(ready.State) == "Ready",
            "Ready branch failed.");
        Check(confirmed.IsSuccess && confirmed.State is RentalState.Confirmed &&
            RentalWorkflow.Describe(confirmed.State) == "Confirmed", "Confirmed branch failed.");
    }

    private static void ConfirmationSuccess()
    {
        var draft = new RentalState.Draft(ValidDraft());
        var prepared = RentalWorkflow.Prepare(draft);
        var result = RentalWorkflow.Confirm(prepared.State);
        Check(result.IsSuccess && result.State is RentalState.Confirmed confirmed &&
            confirmed.Rental.TotalPrice == 1500m, "Confirmation total/state incorrect.");
        Check(draft.Request == ValidDraft() && draft.Request.Game?.AvailableCopies == 3,
            "Confirmation changed the draft or stock snapshot.");
    }

    private static void MissingPrerequisite()
    {
        var draft = new RentalState.Draft(ValidDraft());
        var result = RentalWorkflow.Confirm(draft);
        Check(!result.IsSuccess && result.Error is string error && error.Contains("Prepare"),
            "Unprepared confirmation was not rejected clearly.");
        Check(ReferenceEquals(result.State, draft), "Rejection must preserve the exact original state.");
    }

    private static void RepeatedConfirmation()
    {
        var prepared = RentalWorkflow.Prepare(new RentalState.Draft(ValidDraft()));
        var first = RentalWorkflow.Confirm(prepared.State);
        var second = RentalWorkflow.Confirm(first.State);
        Check(first.IsSuccess && second.IsSuccess && ReferenceEquals(first.State, second.State),
            "Confirmation must be idempotent on a confirmed state.");
        var prepareAgain = RentalWorkflow.Prepare(first.State);
        Check(!prepareAgain.IsSuccess && ReferenceEquals(prepareAgain.State, first.State),
            "Confirmed state must reject preparation without mutation.");
        var readyAgain = RentalWorkflow.Prepare(prepared.State);
        Check(!readyAgain.IsSuccess && ReferenceEquals(readyAgain.State, prepared.State),
            "Ready state must reject repeated preparation.");
    }

    private static void FailedPreparationPreservesState()
    {
        var draft = new RentalState.Draft(ValidDraft() with { Budget = 1m });
        var result = RentalWorkflow.Prepare(draft);
        Check(!result.IsSuccess && result.Error is string error && error.Contains("exceeds budget"),
            "Invalid draft prepared.");
        Check(ReferenceEquals(result.State, draft) && draft.Request.Budget == 1m &&
            draft.Request.Game?.AvailableCopies == 3, "Failed preparation changed state.");
    }
}
