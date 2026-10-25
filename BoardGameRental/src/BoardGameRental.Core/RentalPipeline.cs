namespace BoardGameRental.Core;

public static class RentalPipeline
{
    // Contracts: validated catalog/request and a pure, stable pricing policy.
    public static IReadOnlyList<RentalQuote> Run(
        IReadOnlyList<BoardGame> games,
        RentalRequest request,
        Func<BoardGame, int, decimal> pricePolicy)
    {
        IReadOnlyList<BoardGame> available = SelectAvailableGames(games);
        IReadOnlyList<RentalQuote> quotes = CalculateQuotes(available, request.Days, pricePolicy);
        return BuildRecommendations(quotes, request.Budget);
    }

    public static IReadOnlyList<BoardGame> SelectAvailableGames(
        IReadOnlyList<BoardGame> games)
    {
        return Array.AsReadOnly(games
            .Where(game => game.AvailableCopies > 0)
            .ToArray());
    }

    // This higher-order function actually invokes the supplied delegate.
    public static IReadOnlyList<RentalQuote> CalculateQuotes(
        IReadOnlyList<BoardGame> games,
        int days,
        Func<BoardGame, int, decimal> pricePolicy)
    {
        return Array.AsReadOnly(games
            .Select(game => new RentalQuote(
                game.Id, game.Name, game.Category, days, pricePolicy(game, days)))
            .ToArray());
    }

    public static IReadOnlyList<RentalQuote> BuildRecommendations(
        IReadOnlyList<RentalQuote> quotes,
        decimal budget)
    {
        return Array.AsReadOnly(quotes
            .Where(quote => quote.TotalPrice <= budget)
            .OrderBy(quote => quote.TotalPrice)
            .ThenBy(quote => quote.GameId)
            .ToArray());
    }
}
