using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void TwoPolicies()
    {
        var input = Sample();
        var request = new RentalRequest(3, 3000m);
        var standard = RentalPipeline.Run(input, request, PricingPolicies.Standard);
        var student = RentalPipeline.Run(input, request, PricingPolicies.Student);
        Same(new[] { 4, 2, 1, 6 }, student.Select(q => q.GameId));
        Same(new[] { 0m, 1200m, 2400m, 3000m }, student.Select(q => q.TotalPrice));
        Check(standard.Count == 3 && student.Count == 4, "The policy should change inclusion.");
        Check(standard.Single(q => q.GameId == 1).TotalPrice == 3000m, "Standard Monopoly price.");
        Check(student.Single(q => q.GameId == 1).TotalPrice == 2400m, "Student Monopoly price.");
    }

    private static void Rounding()
    {
        Check(PricingPolicies.Student(new BoardGame(1, "A", "Card", 10.03m, 1), 1) == 8.02m,
            "Student price must round to two decimal places.");
    }
}
