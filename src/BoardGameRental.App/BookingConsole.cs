using System.Globalization;
using BoardGameRental.Core;

namespace BoardGameRental.App;

internal static class BookingConsole
{
    public static int Run(string[] args)
    {
        if (args.Length != 7)
        {
            Console.Error.WriteLine("Use: --book <file> <gameId> <days> <quantity> <budget> <standard|student>");
            return 1;
        }
        if (!int.TryParse(args[2], out int id) || !int.TryParse(args[3], out int days) ||
            !int.TryParse(args[4], out int quantity) ||
            !decimal.TryParse(args[5], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal budget))
        {
            Console.Error.WriteLine("Game ID, days and quantity must be integers; budget must be a number using a dot.");
            return 1;
        }
        if (!CatalogFile.TryLoad(args[1], out var games, out var errors))
        {
            Console.Error.WriteLine(string.Join("; ", errors));
            return 1;
        }
        RentalTariff.TryCreate(args[6], out var tariff);
        var draft = new RentalState.Draft(new RentalDraft(
            games.SingleOrDefault(game => game.Id == id), days, quantity, budget, tariff));
        WorkflowResult prepared = RentalWorkflow.Prepare(draft);
        if (!prepared.IsSuccess)
        {
            Print(prepared);
            return 1;
        }
        WorkflowResult confirmed = RentalWorkflow.Confirm(prepared.State);
        Print(confirmed);
        return confirmed.IsSuccess ? 0 : 1;
    }

    public static int Demo(string path)
    {
        if (!CatalogFile.TryLoad(path, out var games, out var errors))
        {
            Console.Error.WriteLine(string.Join("; ", errors));
            return 1;
        }
        BoardGame? game = games.SingleOrDefault(item => item.Id == 2);
        if (game is null)
        {
            Console.Error.WriteLine("The demo requires game ID 2 from the supplied games.json.");
            return 1;
        }
        var draft = new RentalState.Draft(new RentalDraft(game, 3, 1, 3000m, RentalTariff.Student));
        Console.WriteLine("Missing prerequisite:");
        Print(RentalWorkflow.Confirm(draft));
        Console.WriteLine("Valid operation:");
        WorkflowResult prepared = RentalWorkflow.Prepare(draft);
        if (!prepared.IsSuccess)
        {
            Print(prepared);
            return 1;
        }
        WorkflowResult confirmed = RentalWorkflow.Confirm(prepared.State);
        Print(confirmed);
        Console.WriteLine("Repeated confirmation (same outcome):");
        Print(RentalWorkflow.Confirm(confirmed.State));
        Console.WriteLine("Insufficient stock:");
        Print(RentalWorkflow.Prepare(new RentalState.Draft(draft.Request with { Quantity = 4 })));
        Console.WriteLine("Insufficient budget:");
        Print(RentalWorkflow.Prepare(new RentalState.Draft(draft.Request with { Budget = 100m })));
        return 0;
    }

    private static void Print(WorkflowResult result)
    {
        if (result.Error is string error)
        {
            Console.WriteLine($"Rejected: {error} State: {RentalWorkflow.Describe(result.State)}.");
            return;
        }
        string text = result.State switch
        {
            RentalState.Draft => "Draft: preparation required.",
            RentalState.Ready ready => $"Ready: {ready.Rental.Game.Name}.",
            RentalState.Confirmed confirmed => string.Format(CultureInfo.InvariantCulture,
                "Confirmed: {0}; days: {1}; quantity: {2}; tariff: {3}; total: {4:F2} KZT.",
                confirmed.Rental.Game.Name, confirmed.Rental.Days, confirmed.Rental.Quantity,
                confirmed.Rental.TariffName, confirmed.Rental.TotalPrice),
            _ => throw new ArgumentException("Unknown rental state.")
        };
        Console.WriteLine(text);
    }
}
