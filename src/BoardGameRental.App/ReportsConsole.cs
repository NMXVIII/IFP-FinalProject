using System.Globalization;
using BoardGameRental.Core;

namespace BoardGameRental.App;

internal static class ReportsConsole
{
    public static int Run(string[] args)
    {
        if (args.Length != 1 && args.Length != 6)
        {
            Console.Error.WriteLine("Usage: --reports8 [<catalog> <days> <budget> <targetCopies> <export.json>]");
            return 1;
        }
        bool defaults = args.Length == 1;
        string input = defaults ? Path.Combine(AppContext.BaseDirectory, "sample-data", "games.json") : args[1];
        string output = defaults ? Path.Combine(Environment.CurrentDirectory, "reports.json") : args[5];
        if (!int.TryParse(defaults ? "3" : args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) ||
            !decimal.TryParse(defaults ? "3000" : args[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out decimal budget) ||
            !int.TryParse(defaults ? "2" : args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int target))
        {
            Console.Error.WriteLine("InvalidReportRequest: days and target must be integers; budget uses a decimal point.");
            return 1;
        }

        return ReportRequest.Create(days, budget, target)
            .Bind(request => ReportWorkflow.Run(input, output, request, new NativeStockPlanner()))
            .Match(reports =>
            {
                Show(reports);
                Console.WriteLine($"Exported: {Path.GetFullPath(output)}");
                return 0;
            }, error =>
            {
                Console.Error.WriteLine($"{error.Code}: {error.Message}");
                return 1;
            });
    }

    private static void Show(ReportBundle reports)
    {
        Console.WriteLine($"\nReport 1: affordable one-copy rentals; {reports.Days} days; budget {Money(reports.Budget)} KZT");
        ShowOffers("Standard", reports.StandardOffers);
        ShowOffers("Student (20% discount)", reports.StudentOffers);
        Console.WriteLine($"\nReport 2: inventory by category; target {reports.TargetCopies} copies per title");
        Console.WriteLine("Category | Titles | Available | Daily stock value (KZT) | Copies to buy (native C)");
        foreach (CategorySummary row in reports.Inventory)
            Console.WriteLine($"{row.Category} | {row.Titles} | {row.AvailableCopies} | {Money(row.DailyStockValue)} | {row.CopiesToBuy}");
        if (reports.Inventory.Count == 0) Console.WriteLine("No inventory records.");
    }

    private static void ShowOffers(string label, IReadOnlyList<RentalOffer> offers)
    {
        Console.WriteLine(label);
        foreach (RentalOffer row in offers)
            Console.WriteLine($"{row.GameId} | {row.GameName} | {row.Category} | {row.Days} days | {Money(row.TotalPrice)} KZT");
        if (offers.Count == 0) Console.WriteLine("No matching games.");
    }

    public static int Demo()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BoardGameRentalDemo8-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string data = Path.Combine(AppContext.BaseDirectory, "sample-data");
            Console.WriteLine("Assignment 8 normal workflow: actual JSON, both reports, native C, JSON export.");
            int normal = Run(new[] { "--reports8", Path.Combine(data, "games.json"), "3", "3000", "2", Path.Combine(directory, "reports.json") });
            Console.WriteLine("\nExpected missing-file failure:");
            int missing = Run(new[] { "--reports8", Path.Combine(directory, "missing.json"), "3", "3000", "2", Path.Combine(directory, "missing-report.json") });
            Console.WriteLine("\nExpected malformed-JSON failure:");
            int malformed = Run(new[] { "--reports8", Path.Combine(data, "malformed.json"), "3", "3000", "2", Path.Combine(directory, "malformed-report.json") });
            return normal == 0 && missing == 1 && malformed == 1 ? 0 : 1;
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static string Money(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
