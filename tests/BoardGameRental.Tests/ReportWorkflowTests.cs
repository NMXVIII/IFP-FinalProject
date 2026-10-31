using System.Text.Json;
using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private sealed class A8FixturePlanner : IStockPlanner
    {
        public int Calls { get; private set; }
        public Result<int> Needed(int availableCopies, int targetCopies)
        {
            Calls++;
            return new Result<int>.Success(Math.Max(0, targetCopies - availableCopies));
        }
    }

    private sealed class A8TrackedStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }
        public bool FailWrites { get; init; }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (FailWrites) throw new IOException("Controlled export write failure.");
            base.Write(buffer, offset, count);
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (FailWrites) throw new IOException("Controlled export write failure.");
            base.Write(buffer);
        }
        protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
    }

    private static ReportBundle A8FixtureReports() => A8Value(ReportWorkflow.Build(Sample(), A8Request(), new A8FixturePlanner()));

    private static void A8MaterializedSnapshot()
    {
        int priced = 0;
        var input = Sample().ToList();
        var query = RentalReports.AffordableRentals(input, 3, 3000m,
            game => { priced++; return game.DailyPrice * 3; });
        var snapshot = query.ToArray();
        Check(priced == 5, "Expected one pricing pass on available games.");
        input.Clear();
        Same(new[] { 4, 2, 1 }, snapshot.Select(row => row.GameId));
        _ = snapshot.ToArray();
        Check(priced == 5, "Reading a snapshot must not run the source query again.");
        Check(!query.Any(), "The original deferred query should see the changed source.");
    }

    private static void A8ExportSuccessDisposes()
    {
        var stream = new A8TrackedStream();
        Check(A8Value(ReportExport.WriteAndClose(stream, A8FixtureReports())), "Export did not succeed.");
        Check(stream.WasDisposed && !stream.CanWrite, "Export must dispose its owned stream on success.");
        using var json = JsonDocument.Parse(stream.ToArray());
        Check(json.RootElement.GetProperty("StudentOffers").GetArrayLength() == 4, "Export JSON is incomplete.");
    }

    private static void A8ExportFailureDisposes()
    {
        var stream = new A8TrackedStream { FailWrites = true };
        A7ErrorIs(ReportExport.WriteAndClose(stream, A8FixtureReports()), "ExportFailed");
        Check(stream.WasDisposed && !stream.CanWrite, "Export must dispose its stream after a write exception.");
    }

    private static void A8ExportReplacesExisting() => A7WithTemporaryDirectory(directory =>
    {
        string output = Path.Combine(directory, "report.json");
        File.WriteAllText(output, "old report");
        A8Value(ReportExport.Save(output, A8FixtureReports()));
        using (var read = new FileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(read.Length > 0, "Saved file is empty or still held open.");
        Check(Directory.GetFiles(directory).Length == 1, "Temporary export file was not cleaned.");
        File.Delete(output);
    });

    private static void A8ExportFailurePreservesExisting() => A7WithTemporaryDirectory(directory =>
    {
        string output = Path.Combine(directory, "report.json");
        File.WriteAllText(output, "keep report");
        using (var locked = new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.None))
            A7ErrorIs(ReportExport.Save(output, A8FixtureReports()), "ExportFailed");
        Check(File.ReadAllText(output) == "keep report", "A failed publish changed the previous report.");
        Check(Directory.GetFiles(directory).Length == 1, "Temporary export file remained after failure.");
        File.Delete(output);
    });

    private static void A8WorkflowSnapshot() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "games.json");
        string output = Path.Combine(directory, "report.json");
        File.Copy(SamplePath("games.json"), input);
        string before = File.ReadAllText(input);
        var planner = new A8FixturePlanner();
        ReportBundle reports = A8Value(ReportWorkflow.Run(input, output, A8Request(), planner));
        Check(planner.Calls == 6, "Expected exactly one adapter call per title.");
        _ = reports.Inventory.ToArray();
        _ = reports.StudentOffers.ToArray();
        Check(planner.Calls == 6 && File.ReadAllText(input) == before, "Snapshot reuse repeated I/O or changed the catalog.");
        A7ErrorIs(ReportWorkflow.Run(input, input, A8Request(), planner), "ExportOverwritesInput");
        Check(File.ReadAllText(input) == before, "Source protection failed.");
    });

    private static void A8MissingReportFile() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "missing.json");
        string output = Path.Combine(directory, "report.json");
        var planner = new A8FixturePlanner();
        var result = ReportWorkflow.Run(input, output, A8Request(), planner);
        A7ErrorIs(result, "FileMissing");
        Check(planner.Calls == 0 && !File.Exists(input) && !File.Exists(output), "Missing input continued or created files.");
    });

    private static void A8MalformedReportFile() => A7WithTemporaryDirectory(directory =>
    {
        string input = Path.Combine(directory, "broken.json");
        string output = Path.Combine(directory, "report.json");
        File.WriteAllText(input, "[{ broken");
        File.WriteAllText(output, "previous report");
        var planner = new A8FixturePlanner();
        A7ErrorIs(ReportWorkflow.Run(input, output, A8Request(), planner), "MalformedJson");
        Check(planner.Calls == 0 && File.ReadAllText(output) == "previous report", "Malformed input changed output or called native code.");
        File.Move(input, Path.Combine(directory, "released.json"));
        Check(Directory.GetFiles(directory).Length == 2, "The input handle was not released or extra files were created.");
    });
}
