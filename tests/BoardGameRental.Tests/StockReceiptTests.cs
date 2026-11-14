using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void FinalStockReceiptImmutable()
    {
        var catalog = Sample();
        var before = catalog.ToArray();
        StockReceipt receipt = FinalValue(StockReceiptWorkflow.Receive(catalog, 5, 2));
        Check(receipt.UpdatedGame.Name == "Risk" && receipt.UpdatedGame.AvailableCopies == 2,
            "The received copies were not applied.");
        Same(before, catalog);
        Same(before.Select(game => game.Id), receipt.Catalog.Select(game => game.Id));
        Same(before.Where(game => game.Id != 5), receipt.Catalog.Where(game => game.Id != 5));
    }

    private static void FinalStockReceiptInvalid()
    {
        var catalog = Sample();
        A7ErrorIs(StockReceiptWorkflow.Receive(catalog, 5, 0), "InvalidStockReceipt");
        A7ErrorIs(StockReceiptWorkflow.Receive(catalog, 999, 1), "GameNotFound");
        A7ErrorIs(StockReceiptWorkflow.Receive(catalog, 2, int.MaxValue), "StockOverflow");
    }

    private static void FinalStockReceiptRoundTrip() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "source.json");
        string output = Path.Combine(directory, "received.json");
        File.Copy(SamplePath("games.json"), input);
        byte[] before = File.ReadAllBytes(input);
        StockReceipt receipt = FinalValue(StockReceiptWorkflow.Run(input, 5, 2, output));
        Same(receipt.Catalog, FinalValue(CatalogBoundary.Load(output)));
        Same(before, File.ReadAllBytes(input));
    });

    private static void FinalStockReceiptNoOverwrite() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "source.json");
        string output = Path.Combine(directory, "received.json");
        File.Copy(SamplePath("games.json"), input);
        File.WriteAllText(output, "keep");
        byte[] before = File.ReadAllBytes(input);
        A7ErrorIs(StockReceiptWorkflow.Run(input, 5, 2, output), "CatalogSaveFailed");
        A7ErrorIs(StockReceiptWorkflow.Run(input, 5, 2, input), "CatalogSaveFailed");
        Check(File.ReadAllText(output) == "keep", "Existing destination was overwritten.");
        Same(before, File.ReadAllBytes(input));
        Check(Directory.GetFileSystemEntries(directory).Length == 2, "Temporary files remained.");
    });

    private static void FinalStockReceiptFileFailures() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "source.json");
        string output = Path.Combine(directory, "received.json");
        A7ErrorIs(StockReceiptWorkflow.Run(input, 5, 2, output), "FileMissing");
        File.WriteAllText(input, "[{");
        A7ErrorIs(StockReceiptWorkflow.Run(input, 5, 2, output), "MalformedJson");
        Check(!File.Exists(output), "A failed load reached persistence.");
    });
}
