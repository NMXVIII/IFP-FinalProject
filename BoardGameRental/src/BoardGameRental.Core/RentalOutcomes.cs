using System.Globalization;

namespace BoardGameRental.Core;

public static class RentalOutcomes
{
    public static Result<RentalState> Prepare(RentalState state)
    {
        if (state is not RentalState.Draft draft)
            return Fail("InvalidState", $"Cannot prepare a {RentalWorkflow.Describe(state)} rental.");
        return ValidatedRentalOutcomes.Create(draft.Request)
            .Map<RentalState>(rental => new RentalState.Ready(rental));
    }
    public static Result<RentalState> Confirm(RentalState state)
    {
        WorkflowResult confirmed = RentalWorkflow.Confirm(state);
        return confirmed.Error is string error
            ? Fail("InvalidState", error)
            : new Result<RentalState>.Success(confirmed.State);
    }
    public static Result<string> Book(IReadOnlyList<BoardGame> games, int id, int days,
        int quantity, decimal budget, RentalTariff? tariff) =>
        CatalogLookup.Find(games, id)
            .ToResult(new AppError("GameNotFound", $"Game {id} was not found in the catalog."))
            .Map<RentalState>(game => new RentalState.Draft(new RentalDraft(game, days, quantity, budget, tariff)))
            .Bind(Prepare)
            .Bind(Confirm)
            .Map(DescribeConfirmation);

    private static Result<RentalState> Fail(string code, string message) =>
        new Result<RentalState>.Failure(new AppError(code, message));

    private static string DescribeConfirmation(RentalState state) => state switch
    {
        RentalState.Confirmed confirmed => string.Format(CultureInfo.InvariantCulture,
            "Confirmed: {0}; days: {1}; quantity: {2}; tariff: {3}; total: {4:F2} KZT.",
            confirmed.Rental.Game.Name, confirmed.Rental.Days, confirmed.Rental.Quantity,
            confirmed.Rental.TariffName, confirmed.Rental.TotalPrice),
        _ => throw new InvalidOperationException("A successful booking must be Confirmed.")
    };
}
