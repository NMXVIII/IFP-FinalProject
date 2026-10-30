namespace BoardGameRental.Core;

public sealed record RentalOffer(
    int GameId, string GameName, string Category, int Days, decimal TotalPrice);

public sealed record StockAssessment(BoardGame Game, int CopiesToBuy);

public sealed record CategorySummary(
    string Category, int Titles, long AvailableCopies, decimal DailyStockValue, long CopiesToBuy);

public sealed record ReportBundle(
    int Days, decimal Budget, int TargetCopies,
    IReadOnlyList<RentalOffer> StandardOffers,
    IReadOnlyList<RentalOffer> StudentOffers,
    IReadOnlyList<CategorySummary> Inventory);
