using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static void TariffNames()
    {
        Check(RentalTariff.TryCreate(" STANDARD ", out var standard) && standard == RentalTariff.Standard,
            "Standard selection failed.");
        Check(RentalTariff.TryCreate("Student", out var student) && student == RentalTariff.Student,
            "Student selection failed.");
        foreach (string? name in new string?[] { null, "", "both", "vip" })
            Check(!RentalTariff.TryCreate(name, out var invalid) && invalid is null, "Unknown tariff accepted.");
    }

    private static void QuantityPricing()
    {
        var game = new BoardGame(2, "Uno", "Card", 500m, 3);
        Check(RentalTariff.Standard.Calculate(game, 3, 2) == 3000m, "Standard quantity calculation failed.");
        Check(RentalTariff.Student.Calculate(game, 3, 2) == 2400m, "Student quantity calculation failed.");
    }

    private static void TariffRounding()
    {
        var game = new BoardGame(2, "Uno", "Card", 10.03m, 3);
        Check(RentalTariff.Student.Calculate(game, 1, 3) == 24.06m,
            "Round each copy's rental before multiplying by quantity.");
    }

}
