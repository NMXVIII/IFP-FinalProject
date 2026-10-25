using System.Globalization;
using BoardGameRental.Core;
namespace BoardGameRental.App;

internal static class ExceptionConsole
{
    public static int Run(string[] args)
    {
        if (args.Length != 7 ||
            !int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ||
            !int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) ||
            !int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) ||
            !decimal.TryParse(args[5], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out decimal budget))
        {
            Console.Error.WriteLine("Use: --book7 <file> <gameId> <days> <quantity> <budget> <standard|student>; numbers use a dot.");
            return 1;
        }
        return Print(CatalogBoundary.Load(args[1])
            .Bind(games => RentalTariffOutcomes.Parse(args[6])
                .Bind(tariff => RentalOutcomes.Book(games, id, days, quantity, budget, tariff))));
    }
    // One final outcome handler; domain functions never print or catch business failures.
    private static int Print(Result<string> result) => result.Match(
        text => { Console.WriteLine(text); return 0; },
        error => { Console.WriteLine($"Rejected [{error.Code}]: {error.Message}"); return 1; });

    public static int Demo(string directory)
    {
        string path = Path.Combine(directory, "games.json");
        if (CatalogBoundary.Load(path) is Result<IReadOnlyList<BoardGame>>.Failure error)
            return Print(new Result<string>.Failure(error.Error));
        void Show(string label, int id, int quantity, decimal budget)
        {
            Console.WriteLine(label);
            Print(CatalogBoundary.Load(path).Bind(games =>
                RentalOutcomes.Book(games, id, 3, quantity, budget, RentalTariff.Student)));
        }
        Show("Success / present lookup:", 2, 1, 3000m);
        Show("Missing required lookup:", 999, 1, 3000m);
        Show("Invalid quantity:", 2, 0, 3000m);
        Show("Insufficient stock:", 2, 4, 3000m);
        Show("Budget rejection (retained factory rule):", 2, 1, 100m);
        Console.WriteLine("Unknown tariff:");
        Print(RentalTariffOutcomes.Parse("vip").Map(tariff => tariff.Name));
        Console.WriteLine("Invalid state:");
        Print(RentalOutcomes.Confirm(new RentalState.Draft(new RentalDraft(null, 3, 1, 0m, null)))
            .Map(RentalWorkflow.Describe));
        Console.WriteLine("Present primitive zero:");
        Show("Free Chess at zero budget:", 4, 1, 0m);
        // Controlled directory guarantees a missing file; no laptop-specific assumptions.
        string temporary = Path.Combine(Path.GetTempPath(), "BoardGameRentalDemo-" + Guid.NewGuid());
        Directory.CreateDirectory(temporary);
        try
        {
            Console.WriteLine("Missing file:");
            Print(CatalogBoundary.Load(Path.Combine(temporary, "absent.json")).Map(g => $"Loaded {g.Count}"));
            string malformed = Path.Combine(temporary, "malformed.json");
            File.WriteAllText(malformed, "[{\"Id\":2},");
            Console.WriteLine("Malformed JSON:");
            Print(CatalogBoundary.Load(malformed).Map(g => $"Loaded {g.Count}"));
        }
        finally { Directory.Delete(temporary, true); }
        return 0;
    }
}
