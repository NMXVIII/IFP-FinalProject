using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static RentalDraft ValidDraft() =>
        new(new BoardGame(2, "Uno", "Card", 500m, 3), 3, 1, 3000m, RentalTariff.Standard);

    private static void FactorySuccess()
    {
        Check(ValidatedRental.TryCreate(ValidDraft(), out var rental, out var errors), "Valid rental rejected.");
        Check(rental is not null && rental.Game.Id == 2 && rental.Days == 3 &&
            rental.Quantity == 1 && rental.TotalPrice == 1500m && rental.TariffName == "standard",
            "Factory returned incorrect data.");
        Check(errors.Count == 0, "Success has errors.");
    }

    private static void FactoryRules()
    {
        var draft = ValidDraft();
        (RentalDraft Input, string Reason)[] cases =
        {
            (draft with { Game = null }, "game"),
            (draft with { Game = new BoardGame(0, "Uno", "Card", 500m, 3) }, "Id"),
            (draft with { Game = new BoardGame(2, " ", "Card", 500m, 3) }, "Name"),
            (draft with { Game = new BoardGame(2, "Uno", " ", 500m, 3) }, "Category"),
            (draft with { Game = new BoardGame(2, "Uno", "Card", -1m, 3) }, "DailyPrice"),
            (draft with { Game = new BoardGame(2, "Uno", "Card", 1000001m, 3) }, "DailyPrice"),
            (draft with { Game = new BoardGame(2, "Uno", "Card", 1.001m, 3) }, "decimal"),
            (draft with { Game = new BoardGame(2, "Uno", "Card", 500m, -1) }, "AvailableCopies"),
            (draft with { Days = 0 }, "Days"),
            (draft with { Days = 31 }, "Days"),
            (draft with { Quantity = 0 }, "Quantity"),
            (draft with { Quantity = -1 }, "Quantity"),
            (draft with { Quantity = 4 }, "available"),
            (draft with { Budget = -1m }, "Budget"),
            (draft with { Budget = 1000000001m }, "Budget"),
            (draft with { Budget = 1.001m }, "Budget"),
            (draft with { Tariff = null }, "tariff"),
            (draft with { Budget = 1499.99m }, "exceeds budget")
        };
        foreach (var (input, reason) in cases)
        {
            Check(!ValidatedRental.TryCreate(input, out var rental, out var errors), "Invalid input accepted.");
            Check(rental is null && errors.Any(error => error.Contains(reason, StringComparison.OrdinalIgnoreCase)),
                $"Missing rejection reason: {reason}.");
        }
    }

    private static void FactoryBoundaries()
    {
        var draft = ValidDraft() with { Days = 30, Quantity = 3, Budget = 45000m };
        Check(ValidatedRental.TryCreate(draft, out var rental, out _), "Exact stock/days/budget rejected.");
        Check(rental is not null && rental.TotalPrice == 45000m, "Boundary total incorrect.");
        var free = ValidDraft() with { Game = new BoardGame(4, "Chess", "Classic", 0m, 1), Days = 1, Budget = 0m };
        Check(ValidatedRental.TryCreate(free, out var zero, out _) && zero is not null && zero.TotalPrice == 0m,
            "Free rental rejected.");
        Check(ValidatedRental.TryCreate(ValidDraft() with { Budget = 1000000000m }, out _, out _),
            "Maximum budget rejected.");
    }

    private static void FactoryPreservesInput()
    {
        var draft = ValidDraft() with { Quantity = 4 };
        var before = draft with { };
        Check(!ValidatedRental.TryCreate(draft, out var rental, out _), "Invalid quantity accepted.");
        Check(draft == before && draft.Game == before.Game && rental is null, "Failure changed input.");
        Check(typeof(ValidatedRental).GetConstructors().Length == 0, "Validated value has a public constructor.");
        Check(typeof(ValidatedRental).GetProperties().All(property => !property.CanWrite), "Validated value is writable.");
    }
    private static void StudentBooking()
    {
        var input = new RentalDraft(new BoardGame(2, "Uno", "Card", 500m, 3), 3, 2, 2400m, RentalTariff.Student);
        Check(ValidatedRental.TryCreate(input, out var rental, out _) && rental is not null && rental.TotalPrice == 2400m,
            "Student factory boundary failed.");
        Check(!ValidatedRental.TryCreate(input with { Tariff = RentalTariff.Standard }, out _, out _),
            "Standard tariff should exceed this budget.");
    }
}
