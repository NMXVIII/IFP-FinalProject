using BoardGameRental.Core;
namespace BoardGameRental.Tests;
internal static partial class Program
{
    private static void A7WithTemporaryDirectory(Action<string> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "BoardGameRentalBoundaryTests-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try { test(directory); }
        finally { Directory.Delete(directory, true); }
    }
    private static void A7MissingFilePreservesState() => A7WithTemporaryDirectory(directory =>
    {
        string path = Path.Combine(directory, "absent.json");
        var draft = new RentalState.Draft(A7ValidDraft());
        int calls = 0;
        var result = CatalogBoundary.Load(path).Bind(games => { calls++; return RentalOutcomes.Prepare(draft); });
        A7ErrorIs(result, "FileMissing");
        Check(result is Result<RentalState>.Failure bad && bad.Error.Message.Contains(path), "Missing-file context absent.");
        Check(calls == 0 && !File.Exists(path) && RentalWorkflow.Describe(draft) == "Draft", "Missing file changed state or continued.");
    });
    private static void A7MalformedFilePreservesState() => A7WithTemporaryDirectory(directory =>
    {
        var catalog = Sample();
        var before = catalog.ToArray();
        string path = Path.Combine(directory, "malformed.json");
        var draft = new RentalState.Draft(A7ValidDraft());
        int calls = 0;
        foreach (string json in new[] { "[{\"Id\":2},", "[{\"Id\":\"wrong\"}]" })
        {
            File.WriteAllText(path, json);
            var result = CatalogBoundary.Load(path).Bind(games => { calls++; return RentalOutcomes.Prepare(draft); });
            A7ErrorIs(result, "MalformedJson");
            Check(result is Result<RentalState>.Failure bad && bad.Error.Message.Contains(path), "Parsing context absent.");
            Check(File.ReadAllText(path) == json, "Failed parsing modified stored data.");
        }
        Check(calls == 0 && RentalWorkflow.Describe(draft) == "Draft", "Malformed content caused partial workflow update.");
        Same(before, catalog);
    });
    private static void A7InvalidCatalogAndEmptyFile() => A7WithTemporaryDirectory(directory =>
    {
        string path = Path.Combine(directory, "catalog.json");
        string invalid = "[{\"Id\":1,\"Name\":\"Uno\",\"Category\":\"Card\",\"DailyPrice\":0,\"AvailableCopies\":1},{}]";
        File.WriteAllText(path, invalid);
        var result = CatalogBoundary.Load(path);
        A7ErrorIs(result, "InvalidCatalog");
        Check(File.ReadAllText(path) == invalid, "Invalid catalog was modified.");
        File.WriteAllText(path, "[]");
        Check(CatalogBoundary.Load(path) is Result<IReadOnlyList<BoardGame>>.Success empty && empty.Value.Count == 0, "Valid empty catalog rejected.");
    });
    private static void A7CompleteFileToBookingChain() => A7WithTemporaryDirectory(directory =>
    {
        string path = Path.Combine(directory, "games.json");
        File.Copy(SamplePath("games.json"), path);
        string before = File.ReadAllText(path);
        var result = CatalogBoundary.Load(path)
            .Bind(games => RentalTariffOutcomes.Parse("student")
                .Bind(tariff => RentalOutcomes.Book(games, 2, 3, 1, 3000m, tariff)));
        Check(result is Result<string>.Success ok && ok.Value.Contains("total: 1200.00 KZT"), "Complete boundary chain incorrect.");
        Check(File.ReadAllText(path) == before, "Successful booking modified stored data.");
        int calls = 0;
        var unsupported = CatalogBoundary.Load(path).Bind(games => RentalTariffOutcomes.Parse("vip")
            .Bind(tariff => { calls++; return RentalOutcomes.Book(games, 2, 3, 1, 3000m, tariff); }));
        A7ErrorIs(unsupported, "UnknownTariff");
        Check(calls == 0, "Unknown tariff did not stop booking.");
    });
}
