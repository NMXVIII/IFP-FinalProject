namespace BoardGameRental.Core;

public sealed record RentalDraft(
    BoardGame? Game,
    int Days,
    int Quantity,
    decimal Budget,
    RentalTariff? Tariff);

public sealed class ValidatedRental
{
    public BoardGame Game { get; }
    public int Days { get; }
    public int Quantity { get; }
    public decimal Budget { get; }
    public string TariffName { get; }
    public decimal TotalPrice { get; }

    private ValidatedRental(BoardGame game, RentalDraft draft, RentalTariff tariff, decimal total)
    {
        Game = game;
        Days = draft.Days;
        Quantity = draft.Quantity;
        Budget = draft.Budget;
        TariffName = tariff.Name;
        TotalPrice = total;
    }

    public static bool TryCreate(RentalDraft draft, out ValidatedRental? rental,
        out IReadOnlyList<string> errors)
    {
        rental = null;
        var messages = new List<string>();
        if (draft.Game is null)
        {
            messages.Add("An existing catalog game is required.");
        }
        else
        {
            var game = draft.Game;
            RawGame[] raw = { new(game.Id, game.Name, game.Category, game.DailyPrice, game.AvailableCopies) };
            if (!InputValidation.TryValidateCatalog(raw, out _, out var catalogErrors))
                messages.AddRange(catalogErrors);
        }
        if (draft.Days < 1 || draft.Days > 30)
            messages.Add("Days must be from 1 to 30.");
        if (draft.Quantity < 1)
            messages.Add("Quantity must be positive.");
        if (draft.Game is BoardGame selected && draft.Quantity > selected.AvailableCopies)
            messages.Add("Quantity exceeds available copies.");
        if (draft.Budget < 0 || draft.Budget > 1000000000m || decimal.Round(draft.Budget, 2) != draft.Budget)
            messages.Add("Budget must be from 0 to 1000000000 with at most two decimal places.");
        if (draft.Tariff is null)
            messages.Add("A standard or student tariff is required.");

        if (messages.Count > 0)
        {
            errors = Array.AsReadOnly(messages.ToArray());
            return false;
        }
        if (draft.Game is not BoardGame validGame || draft.Tariff is not RentalTariff validTariff)
        {
            errors = new[] { "Game and tariff are required." };
            return false;
        }
        decimal total = validTariff.Calculate(validGame, draft.Days, draft.Quantity);
        if (total > draft.Budget)
        {
            errors = new[] { "Total price exceeds budget." };
            return false;
        }
        rental = new ValidatedRental(validGame, draft, validTariff, total);
        errors = Array.Empty<string>();
        return true;
    }
}
