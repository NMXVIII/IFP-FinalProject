namespace BoardGameRental.Core;

public static class CategoryReports
{
    // Native results are supplied as data; grouping itself is a pure transformation.
    public static IReadOnlyList<CategorySummary> Inventory(IEnumerable<StockAssessment> stock) =>
        Array.AsReadOnly(stock
            .GroupBy(item => item.Game.Category, StringComparer.Ordinal)
            .Select(group => new CategorySummary(
                group.Key,
                group.Count(),
                group.Sum(item => (long)item.Game.AvailableCopies),
                group.Sum(item => item.Game.DailyPrice * item.Game.AvailableCopies),
                group.Sum(item => (long)item.CopiesToBuy)))
            .OrderByDescending(row => row.DailyStockValue)
            .ThenBy(row => row.Category, StringComparer.Ordinal)
            .ToArray());
}
