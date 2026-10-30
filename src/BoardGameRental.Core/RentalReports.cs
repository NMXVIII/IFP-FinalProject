namespace BoardGameRental.Core;

public static class RentalReports
{
    // Inputs have been validated by ReportWorkflow. No I/O or diagnostic effects.
    // OrderBy must inspect the source before it can yield the first sorted offer.
    public static IEnumerable<RentalOffer> AffordableRentals(
        IEnumerable<BoardGame> games, int days, decimal budget,
        Func<BoardGame, decimal> configuredPrice)
    {
        foreach (RentalOffer offer in games.AvailableForRental()
            .Select(game => game.QuoteFor(days, configuredPrice))
            .Where(offer => offer.TotalPrice <= budget)
            .OrderBy(offer => offer.TotalPrice)
            .ThenBy(offer => offer.GameId))
        {
            yield return offer;
        }
    }
}
