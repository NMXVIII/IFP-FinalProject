using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void LegitimateZeros()
    {
        RawGame?[] raw = { ValidRaw() with { DailyPrice = 0m }, ValidRaw() with { Id = 2, AvailableCopies = 0 } };
        Check(InputValidation.TryValidateCatalog(raw, out var games, out _), "Zero values are valid.");
        var result = RentalPipeline.Run(games, new RentalRequest(1, 0m), PricingPolicies.Standard);
        Check(result.Count == 1 && result[0].GameId == 1 && result[0].TotalPrice == 0m,
            "Free game must remain available; zero-stock game must be excluded.");
    }

    private static void MissingInput()
    {
        Check(!InputValidation.TryValidateCatalog(null, out var missing, out var errors), "Null catalog accepted.");
        Check(missing.Count == 0 && errors.Count > 0, "Missing input must not fabricate a result.");
        Check(!InputValidation.TryValidateCatalog(new RawGame?[] { null }, out _, out _), "Null record accepted.");
        Check(!CatalogFile.TryLoad(SamplePath("missing-catalog.json"), out _, out _), "JSON null accepted.");
    }

    private static void InvalidFields()
    {
        RawGame[] invalid =
        {
            ValidRaw() with { Id = null }, ValidRaw() with { Id = 0 },
            ValidRaw() with { Name = null }, ValidRaw() with { Name = " " },
            ValidRaw() with { Category = null }, ValidRaw() with { Category = " " },
            ValidRaw() with { DailyPrice = null }, ValidRaw() with { DailyPrice = -1m },
            ValidRaw() with { DailyPrice = 1000001m }, ValidRaw() with { DailyPrice = 1.001m },
            ValidRaw() with { AvailableCopies = null }, ValidRaw() with { AvailableCopies = -1 }
        };
        foreach (RawGame raw in invalid)
        {
            Check(!InputValidation.TryValidateCatalog(new[] { raw }, out var games, out var errors),
                $"Invalid record accepted: {raw}");
            Check(games.Count == 0 && errors.Count > 0, "Invalid record returned partial data.");
        }
        Check(InputValidation.TryValidateCatalog(new[] { ValidRaw() with { DailyPrice = 1000000m } }, out _, out _),
            "Maximum daily price should be included.");
    }

    private static void DuplicateIds()
    {
        Check(!InputValidation.TryValidateCatalog(new[] { ValidRaw(), ValidRaw() }, out _, out var errors),
            "Duplicate ID accepted.");
        Check(errors.Any(e => e.Contains("duplicate Id")), "Duplicate error missing.");
    }

    private static void RequestValidation()
    {
        Check(InputValidation.TryParseRequest("1", "0", out var minimum, out _) && minimum == new RentalRequest(1, 0m),
            "Minimum request rejected.");
        Check(InputValidation.TryParseRequest("30", "1000000000", out _, out _), "Maximum request rejected.");
        foreach (string? days in new string?[] { null, "", "abc", "1.5", "0", "31", "-1" })
            Check(!InputValidation.TryParseRequest(days, "3000", out var value, out _) && value is null,
                "Invalid days accepted.");
        foreach (string? budget in new string?[] { null, "", "abc", "-1", "1.001", "1000000001", "1,50" })
            Check(!InputValidation.TryParseRequest("3", budget, out var value, out _) && value is null,
                "Invalid budget accepted.");
    }
}
