namespace BoardGameRental.Core;

public static class ReportExtensions
{
    // Filtering belongs on the sequence; its source is never changed.
    public static IEnumerable<BoardGame> AvailableForRental(this IEnumerable<BoardGame> games) =>
        games.Where(game => game.AvailableCopies > 0);

    // A game supplies the identity and category needed for a one-copy offer.
    public static RentalOffer QuoteFor(
        this BoardGame game, int days, Func<BoardGame, decimal> configuredPrice) =>
        new(game.Id, game.Name, game.Category, days, configuredPrice(game));
}
