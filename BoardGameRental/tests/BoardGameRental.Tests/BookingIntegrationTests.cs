using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void CatalogBooking()
    {
        var games = Sample();
        var game = games.Single(item => item.Id == 2);
        Check(RentalTariff.TryCreate("student", out var tariff), "Tariff selection failed.");
        var prepared = RentalWorkflow.Prepare(new RentalState.Draft(new RentalDraft(game, 3, 1, 3000m, tariff)));
        var confirmed = RentalWorkflow.Confirm(prepared.State);
        Check(prepared.IsSuccess && confirmed.IsSuccess && confirmed.State is RentalState.Confirmed booking &&
            booking.Rental.Game.Name == "Uno" && booking.Rental.TotalPrice == 1200m, "Catalog workflow failed.");
    }

    private static void UnknownCatalogGame()
    {
        var game = Sample().SingleOrDefault(item => item.Id == 999);
        var draft = new RentalState.Draft(new RentalDraft(game, 3, 1, 3000m, RentalTariff.Standard));
        var result = RentalWorkflow.Prepare(draft);
        Check(!result.IsSuccess && ReferenceEquals(result.State, draft) &&
            result.Error is string error && error.Contains("game"), "Missing catalog game accepted.");
    }

    private static void FileAndCatalogPreserved()
    {
        string path = SamplePath("games.json");
        byte[] before = File.ReadAllBytes(path);
        var games = Sample();
        BoardGame[] snapshot = games.ToArray();
        var draft = new RentalState.Draft(new RentalDraft(games[0], 3, 99, 3000m, RentalTariff.Standard));
        var failure = RentalWorkflow.Prepare(draft);
        Check(!failure.IsSuccess && ReferenceEquals(failure.State, draft), "Expected capacity failure.");
        Same(snapshot, games);
        Same(before, File.ReadAllBytes(path));
    }
}
