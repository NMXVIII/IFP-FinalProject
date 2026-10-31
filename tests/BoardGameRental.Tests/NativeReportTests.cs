using System.Text.Json;
using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void A8NativeRealCall()
    {
        var native = new NativeStockPlanner();
        Check(A8Value(native.Needed(1, 2)) == 1, "Manual example: target 2 minus available 1 must be 1.");
        Check(A8Value(native.Needed(3, 2)) == 0 && A8Value(native.Needed(0, 2)) == 2, "Real native stock boundaries are incorrect.");
        Check(A8Value(native.Needed(0, int.MaxValue)) == int.MaxValue && A8Value(native.Needed(int.MaxValue, 0)) == 0,
            "Fixed-width integer extremes must not overflow.");
    }

    private static void A8NativeInvalidInputs()
    {
        int calls = 0;
        var native = new NativeStockPlanner((_, _) => { calls++; return 0; });
        A7ErrorIs(native.Needed(-1, 2), "InvalidStock");
        A7ErrorIs(native.Needed(1, -1), "InvalidStock");
        Check(calls == 0, "Invalid stock must be rejected before crossing the native boundary.");
    }

    private static void A8NativeLoadFailure() => A7ErrorIs(
        new NativeStockPlanner((_, _) => throw new DllNotFoundException("Controlled absent library.")).Needed(1, 2),
        "NativeLibraryMissing");

    private static void A8NativeBindingFailures()
    {
        A7ErrorIs(new NativeStockPlanner((_, _) => throw new EntryPointNotFoundException("Controlled absent export.")).Needed(1, 2),
            "NativeEntryPointMissing");
        A7ErrorIs(new NativeStockPlanner((_, _) => throw new BadImageFormatException("Controlled incompatible binary.")).Needed(1, 2),
            "NativeArchitectureMismatch");
    }

    private static void A8NativeSentinel()
    {
        A7ErrorIs(new NativeStockPlanner((_, _) => -1).Needed(1, 2), "NativeCallFailed");
        A7ErrorIs(new NativeStockPlanner((_, _) => 3).Needed(1, 2), "NativeCallFailed");
    }

    private static void A8FullNativeReportWorkflow() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "games.json");
        string output = Path.Combine(directory, "reports.json");
        File.Copy(SamplePath("games.json"), input);
        string before = File.ReadAllText(input);
        ReportBundle reports = A8Value(ReportWorkflow.Run(input, output, A8Request(), new NativeStockPlanner()));
        Same(new[] { 4, 2, 1 }, reports.StandardOffers.Select(row => row.GameId));
        Same(new[] { 0m, 1200m, 2400m, 3000m }, reports.StudentOffers.Select(row => row.TotalPrice));
        Check(reports.Inventory[0] == new CategorySummary("Strategy", 3, 3, 4000m, 3), "Native result was not used in the category report.");
        using (var json = JsonDocument.Parse(File.ReadAllText(output)))
        {
            var rows = json.RootElement.GetProperty("Inventory").EnumerateArray();
            Check(rows.Sum(row => row.GetProperty("CopiesToBuy").GetInt64()) == 4, "Native totals were not exported.");
        }
        Check(File.ReadAllText(input) == before && Directory.GetFiles(directory).Length == 2, "The successful workflow changed the source or left temporary files.");
        File.Delete(output);
        File.Delete(input);
    });

    private static void A8NativeFailurePreservesState() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "games.json");
        string output = Path.Combine(directory, "reports.json");
        File.Copy(SamplePath("games.json"), input);
        File.WriteAllText(output, "keep report");
        string before = File.ReadAllText(input);
        var draft = new RentalState.Draft(A7ValidDraft());
        var failed = new NativeStockPlanner((_, _) => throw new DllNotFoundException("Controlled integration failure."));
        A7ErrorIs(ReportWorkflow.Run(input, output, A8Request(), failed), "NativeLibraryMissing");
        Check(File.ReadAllText(input) == before && File.ReadAllText(output) == "keep report", "Native failure changed persistent state.");
        Check(RentalWorkflow.Describe(draft) == "Draft" && Directory.GetFiles(directory).Length == 2, "Native failure changed rental state or left partial files.");
    });

    private static void A8NativeEmptyWorkflow()
    {
        int calls = 0;
        var native = new NativeStockPlanner((_, _) => { calls++; throw new DllNotFoundException(); });
        var empty = A8Value(ReportWorkflow.Build(Array.Empty<BoardGame>(), A8Request(), native));
        Check(calls == 0 && empty.StandardOffers.Count == 0 && empty.StudentOffers.Count == 0 && empty.Inventory.Count == 0,
            "An empty report must not invoke the adapter.");
        A7ErrorIs(ReportWorkflow.Build(new[] { new BoardGame(1, "Bad", "Test", -1m, 1) }, A8Request(), native), "InvalidCatalog");
        Check(calls == 0, "Invalid catalog reached the native boundary.");
    }
}
