using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void NormalWorkflow()
    {
        var actual = RentalPipeline.Run(Sample(), new RentalRequest(3, 3000m), PricingPolicies.Standard);
        Same(new[] { 4, 2, 1 }, actual.Select(q => q.GameId));
        Same(new[] { 0m, 1500m, 3000m }, actual.Select(q => q.TotalPrice));
        Check(actual.All(q => q.Days == 3), "Rental days were lost.");
        Check(actual[2].GameName == "Monopoly", "Wrong game name.");
    }

    private static void BudgetBoundary()
    {
        BoardGame[] games = { new(1, "A", "Family", 1000m, 1), new(2, "B", "Family", 1000.01m, 1) };
        var result = RentalPipeline.Run(games, new RentalRequest(3, 3000m), PricingPolicies.Standard);
        Same(new[] { 1 }, result.Select(q => q.GameId));
    }

    private static void AvailabilityStage()
    {
        Same(new[] { 1, 2, 3, 4, 6 }, RentalPipeline.SelectAvailableGames(Sample()).Select(g => g.Id));
    }

    private static void DelegateInvocation()
    {
        int calls = 0;
        // Test-only probe: production pricing policies have no side effects.
        decimal Probe(BoardGame game, int days)
        {
            calls++;
            return game.DailyPrice * days + 10m;
        }
        var result = RentalPipeline.Run(Sample(), new RentalRequest(2, 10000m), Probe);
        Check(calls == 5, "Delegate must run once per selected game.");
        Check(result.Single(q => q.GameId == 1).TotalPrice == 2010m, "Delegate result unused.");
    }

    private static void PurityAndOwnership()
    {
        var source = Sample().ToList();
        BoardGame[] before = source.ToArray();
        var request = new RentalRequest(3, 3000m);
        var first = RentalPipeline.Run(source, request, PricingPolicies.Standard);
        var second = RentalPipeline.Run(source, request, PricingPolicies.Standard);
        Same(before, source);
        Same(first, second);
        var selected = RentalPipeline.SelectAvailableGames(source);
        source.Clear();
        Check(selected.Count == 5 && first.Count == 3, "Results share a mutable caller collection.");
        Check(first is ICollection<RentalQuote> collection && collection.IsReadOnly,
            "Result collection must be read-only.");
        RawGame?[] raw = { ValidRaw() };
        Check(InputValidation.TryValidateCatalog(raw, out var validated, out _), "Validation failed.");
        raw[0] = ValidRaw() with { Name = "Changed" };
        Check(validated[0].Name == "Uno", "Validated data shares mutable raw storage.");
    }

    private static void StableOrdering()
    {
        RentalQuote[] quotes = { new(9, "B", "Card", 1, 10m), new(2, "A", "Card", 1, 10m) };
        Same(new[] { 2, 9 }, RentalPipeline.BuildRecommendations(quotes, 10m).Select(q => q.GameId));
        Same(new[] { 9, 2 }, quotes.Select(q => q.GameId));
    }

    private static void NoMatches()
    {
        BoardGame[] games = { new(1, "A", "Card", 100m, 1) };
        Check(RentalPipeline.Run(games, new RentalRequest(1, 0m), PricingPolicies.Standard).Count == 0,
            "No matches should be a successful empty result.");
    }
}
