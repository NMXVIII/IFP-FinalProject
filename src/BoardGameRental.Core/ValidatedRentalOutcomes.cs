namespace BoardGameRental.Core;

// Validates the raw request before a Ready state can exist.
public static class ValidatedRentalOutcomes
{
    public static Result<ValidatedRental> Create(RentalDraft draft)
    {
        if (draft.Game is null)
            return Fail("GameNotFound", "An existing catalog game is required.");
        if (draft.Quantity <= 0)
            return Fail("InvalidQuantity", $"Quantity {draft.Quantity} must be positive.");
        if (draft.Quantity > draft.Game.AvailableCopies)
            return Fail("InsufficientStock", $"Game {draft.Game.Id}: requested {draft.Quantity}, available {draft.Game.AvailableCopies}.");
        // Keep all Assignment 6 validation and pricing rules in the existing factory.
        if (!ValidatedRental.TryCreate(draft, out var rental, out var errors))
            return Fail("ValidationRejected", string.Join("; ", errors));
        if (rental is not ValidatedRental validated)
            throw new InvalidOperationException("The successful factory must return a validated rental.");
        return new Result<ValidatedRental>.Success(validated);
    }
    private static Result<ValidatedRental> Fail(string code, string message) =>
        new Result<ValidatedRental>.Failure(new AppError(code, message));
}
