using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static ReportRequest A8Request() => A8Value(ReportRequest.Create(3, 3000m, 2));

    private static StockAssessment[] A8AssessedSample() => Sample().Select(game =>
        new StockAssessment(game, game.Id switch { 3 => 1, 4 => 1, 5 => 2, _ => 0 })).ToArray();

    private static void A8ConfiguredSameInput()
    {
        var standard = ReportPricing.CreatePrice(RentalTariff.Standard)(3);
        var student = ReportPricing.CreatePrice(RentalTariff.Student)(3);
        BoardGame uno = Sample().Single(game => game.Id == 2);
        Check(standard(uno) == 1500m && student(uno) == 1200m, "The same Uno input must produce different configured prices.");
        Same(new[] { 4, 2, 1 }, RentalReports.AffordableRentals(Sample(), 3, 3000m, standard).Select(row => row.GameId));
        Same(new[] { 4, 2, 1, 6 }, RentalReports.AffordableRentals(Sample(), 3, 3000m, student).Select(row => row.GameId));
        Check(standard(uno) == 1500m, "Using another configuration must not change the first closure.");
    }

    private static void A8ConfiguredRounding()
    {
        var game = new BoardGame(1, "Tiny", "Test", 0.03m, 1);
        Check(ReportPricing.CreatePrice(RentalTariff.Standard)(3)(game) == 0.09m, "Wrong standard rounding.");
        Check(ReportPricing.CreatePrice(RentalTariff.Student)(3)(game) == 0.07m, "Wrong discounted two-decimal rounding.");
    }

    private static void A8CategoryTotals()
    {
        var rows = CategoryReports.Inventory(A8AssessedSample());
        Same(new[] { "Strategy", "Family", "Card", "Classic" }, rows.Select(row => row.Category));
        Check(rows[0] == new CategorySummary("Strategy", 3, 3, 4000m, 3), "Strategy totals are incorrect.");
        Check(rows[1] == new CategorySummary("Family", 1, 2, 2000m, 0), "Family totals are incorrect.");
        Check(rows[2] == new CategorySummary("Card", 1, 3, 1500m, 0), "Card totals are incorrect.");
        Check(rows[3] == new CategorySummary("Classic", 1, 1, 0m, 1), "Classic totals are incorrect.");
        Check(rows.Sum(row => row.DailyStockValue) == 7500m && rows.Sum(row => row.CopiesToBuy) == 4, "Overall totals are incorrect.");
    }

    private static void A8CategoryEmpty() =>
        Check(CategoryReports.Inventory(Array.Empty<StockAssessment>()).Count == 0, "Empty category report is incorrect.");

    private static void A8CategoryTies()
    {
        var input = new[]
        {
            new StockAssessment(new BoardGame(1, "Z", "Zulu", 100m, 1), 1),
            new StockAssessment(new BoardGame(2, "A", "Alpha", 50m, 2), 0)
        };
        Same(new[] { "Alpha", "Zulu" }, CategoryReports.Inventory(input).Select(row => row.Category));
        Same(new[] { "Alpha", "Zulu" }, CategoryReports.Inventory(input.Reverse()).Select(row => row.Category));
    }

    private static void A8CategoryZeros()
    {
        var input = new[]
        {
            new StockAssessment(new BoardGame(1, "Free", "Same", 0m, 2), 0),
            new StockAssessment(new BoardGame(2, "Sold out", "Same", 100m, 0), 2)
        };
        Check(CategoryReports.Inventory(input).Single() == new CategorySummary("Same", 2, 2, 0m, 2),
            "Zero price and zero stock must still count as inventory titles.");
    }

    private static void A8ReportRequestRules()
    {
        Check(ReportRequest.Create(1, 0m, 0) is Result<ReportRequest>.Success, "Valid zeros rejected.");
        Check(ReportRequest.Create(30, 1000000000m, int.MaxValue) is Result<ReportRequest>.Success, "Valid upper bounds rejected.");
        foreach (var input in new[] { (0, 0m, 2), (31, 0m, 2), (1, -1m, 2), (1, 1000000001m, 2), (1, 0.001m, 2), (1, 0m, -1) })
            A7ErrorIs(ReportRequest.Create(input.Item1, input.Item2, input.Item3), "InvalidReportRequest");
    }

    private static void A8InvalidCatalogRejected()
    {
        A7ErrorIs(ReportInput.Validate(null), "InvalidCatalog");
        foreach (var game in new[]
        {
            new BoardGame(0, "Uno", "Card", 500m, 1), new BoardGame(1, " ", "Card", 500m, 1),
            new BoardGame(1, "Uno", "", 500m, 1), new BoardGame(1, "Uno", "Card", -1m, 1),
            new BoardGame(1, "Uno", "Card", 0.001m, 1), new BoardGame(1, "Uno", "Card", 500m, -1)
        }) A7ErrorIs(ReportInput.Validate(new[] { game }), "InvalidCatalog");
        var duplicate = new BoardGame(1, "Uno", "Card", 500m, 1);
        A7ErrorIs(ReportInput.Validate(new[] { duplicate, duplicate }), "InvalidCatalog");
        Check(A8Value(ReportInput.Validate(Array.Empty<BoardGame>())).Count == 0, "Valid empty input rejected.");
    }
}
