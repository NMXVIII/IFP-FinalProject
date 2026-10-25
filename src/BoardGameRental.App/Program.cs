using System.Globalization;
using BoardGameRental.Core;

namespace BoardGameRental.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine("BoardGameRental - Assignments 5, 6 and 7");
        if (args.Length > 0 && args[0] == "--book7")
            return ExceptionConsole.Run(args);
        if (args.Length == 1 && args[0] == "--demo7")
            return ExceptionConsole.Demo(Path.Combine(AppContext.BaseDirectory, "sample-data"));
        if (args.Length > 0 && args[0] == "--book")
            return BookingConsole.Run(args);
        if (args.Length == 1 && args[0] == "--demo")
            return BookingConsole.Demo(Path.Combine(AppContext.BaseDirectory, "sample-data", "games.json"));
        if (args.Length == 1 && args[0] == "--help")
        {
            Console.WriteLine("Default: run both policies on games.json for 3 days with a 3000 KZT budget.");
            Console.WriteLine("Custom: dotnet run --project src/BoardGameRental.App -- <file> <days> <budget> <both|standard|student>");
            Console.WriteLine("Assignment 7: --demo7 or --book7 <file> <gameId> <days> <quantity> <budget> <standard|student>");
            Console.WriteLine("Assignment 6 demo: --demo");
            Console.WriteLine("Booking: --book <file> <gameId> <days> <quantity> <budget> <standard|student>");
            return 0;
        }
        if (args.Length != 0 && args.Length != 4)
        {
            Console.Error.WriteLine("Provide all four arguments: file days budget policy. Use --help.");
            return 1;
        }

        string path = args.Length == 0
            ? Path.Combine(AppContext.BaseDirectory, "sample-data", "games.json") : args[0];
        string daysText = args.Length == 0 ? "3" : args[1];
        string budgetText = args.Length == 0 ? "3000" : args[2];
        string policyName = args.Length == 0 ? "both" : args[3].ToLowerInvariant();

        if (!InputValidation.TryParseRequest(daysText, budgetText, out var request, out string error)
            || request is null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        if (policyName != "both" && policyName != "standard" && policyName != "student")
        {
            Console.Error.WriteLine("Policy must be both, standard, or student.");
            return 1;
        }
        if (!CatalogFile.TryLoad(path, out var games, out var errors))
        {
            foreach (string message in errors)
                Console.Error.WriteLine(message);
            return 1;
        }

        Console.WriteLine($"Catalog records: {games.Count}; days: {request.Days}; budget: {Money(request.Budget)} KZT");
        if (policyName is "both" or "standard")
            ShowRecommendations("Standard", games, request, PricingPolicies.Standard);
        if (policyName is "both" or "student")
            ShowRecommendations("Student (20% discount)", games, request, PricingPolicies.Student);
        return 0;
    }

    private static void ShowRecommendations(
        string title,
        IReadOnlyList<BoardGame> games,
        RentalRequest request,
        Func<BoardGame, int, decimal> pricePolicy)
    {
        // Compute first, then display. Display is not a pure pipeline stage.
        IReadOnlyList<RentalQuote> result = RentalPipeline.Run(games, request, pricePolicy);
        Console.WriteLine($"\n{title}");
        if (result.Count == 0)
        {
            Console.WriteLine("No matching games. This is a valid empty result.");
            return;
        }
        foreach (RentalQuote quote in result)
            Console.WriteLine($"{quote.GameId,2} | {quote.GameName,-15} | {quote.Category,-10} | {quote.Days} days | {Money(quote.TotalPrice)} KZT");
    }

    private static string Money(decimal value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
