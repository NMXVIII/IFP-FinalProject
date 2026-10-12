using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void EmptyDataset()
    {
        Check(CatalogFile.TryLoad(SamplePath("empty.json"), out var games, out _), "[] must be valid.");
        Check(RentalPipeline.Run(games, new RentalRequest(1, 0m), PricingPolicies.Standard).Count == 0,
            "Empty catalog must give an empty result.");
    }

    private static void InvalidFile()
    {
        Check(!CatalogFile.TryLoad(SamplePath("invalid.json"), out var games, out var errors), "Invalid file accepted.");
        Check(games.Count == 0 && errors.Count >= 3, "Invalid file must report errors without partial success.");
    }

    private static void MalformedFile()
    {
        Check(!CatalogFile.TryLoad(SamplePath("malformed.json"), out _, out var errors), "Malformed JSON accepted.");
        Check(errors.Any(e => e.Contains("Invalid JSON")), "Parsing error missing.");
    }

    private static void MissingFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BoardGameRentalTests-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            Check(!CatalogFile.TryLoad(Path.Combine(directory, "absent.json"), out var games, out var errors),
                "Missing file accepted.");
            Check(games.Count == 0 && errors.Count > 0, "Missing-file failure unclear.");
        }
        finally
        {
            Directory.Delete(directory);
        }
    }
}
