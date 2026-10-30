using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static T A8Value<T>(Result<T> result) where T : notnull => result.Match(
        value => value, error => throw new InvalidOperationException($"{error.Code}: {error.Message}"));

    private static void A8StockExtension()
    {
        var games = Sample();
        var before = games.ToArray();
        Same(new[] { 1, 2, 3, 4, 6 }, games.AvailableForRental().Select(game => game.Id));
        Same(before, games);
        Check(games.AvailableForRental().Any(game => game.DailyPrice == 0), "Free games must remain available.");
    }

    private static void A8QuoteExtension()
    {
        BoardGame uno = Sample().Single(game => game.Id == 2);
        int calls = 0;
        RentalOffer offer = uno.QuoteFor(3, game => { calls++; return game.DailyPrice * 3; });
        Check(offer == new RentalOffer(2, "Uno", "Card", 3, 1500m), "Offer lost identity, duration or price.");
        Check(calls == 1 && uno.DailyPrice == 500m, "Projection must call its function once without mutation.");
    }

    private static void A8AffordableNormal()
    {
        var result = RentalReports.AffordableRentals(Sample(), 3, 3000m, game => game.DailyPrice * 3).ToArray();
        Same(new[] { 4, 2, 1 }, result.Select(row => row.GameId));
        Same(new[] { 0m, 1500m, 3000m }, result.Select(row => row.TotalPrice));
        Check(result.All(row => row.Days == 3), "Report duration is incorrect.");
    }

    private static void A8AffordableEmpty() =>
        Check(!RentalReports.AffordableRentals(Array.Empty<BoardGame>(), 3, 3000m, game => game.DailyPrice * 3).Any(),
            "An empty catalog must produce an empty report.");

    private static void A8AffordableNoMatches()
    {
        var games = new[] { new BoardGame(1, "Paid", "Test", 1m, 1), new BoardGame(2, "Unavailable", "Test", 0m, 0) };
        Check(!RentalReports.AffordableRentals(games, 3, 0m, game => game.DailyPrice * 3).Any(), "Unavailable free games are not offers.");
    }

    private static void A8AffordableTies()
    {
        var games = new[] { new BoardGame(9, "Later", "Test", 100m, 1), new BoardGame(2, "First", "Test", 100m, 1) };
        Same(new[] { 2, 9 }, RentalReports.AffordableRentals(games, 1, 100m, game => game.DailyPrice).Select(row => row.GameId));
        Same(new[] { 9, 2 }, games.Select(game => game.Id));
    }

    private static void A8DeferredTiming()
    {
        int read = 0, priced = 0;
        IEnumerable<BoardGame> Probe()
        {
            foreach (BoardGame game in Sample()) { read++; yield return game; }
        }
        var deferred = RentalReports.AffordableRentals(Probe(), 3, 3000m,
            game => { priced++; return game.DailyPrice * 3; });
        Check(read == 0 && priced == 0, "Creating the enumerable must not process records.");
        using (var iterator = deferred.GetEnumerator())
        {
            Check(read == 0 && priced == 0, "Getting the iterator must remain deferred.");
            Check(iterator.MoveNext() && iterator.Current.GameId == 4, "Expected the free offer first.");
            Check(read == 6 && priced == 5, "Sorting must read all input and price only available titles.");
        }
        _ = deferred.ToArray();
        Check(read == 12 && priced == 10, "A deferred query must recompute on a second enumeration.");
    }

    private static void A8DeferredEarlyStop()
    {
        bool released = false;
        IEnumerable<BoardGame> Probe()
        {
            try { foreach (BoardGame game in Sample()) yield return game; }
            finally { released = true; }
        }
        foreach (var row in RentalReports.AffordableRentals(Probe(), 3, 3000m, game => game.DailyPrice * 3))
        {
            Check(row.GameId == 4, "Wrong sorted first offer.");
            break;
        }
        Check(released, "The source enumerator must be disposed when processing stops.");
    }
}
