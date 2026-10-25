using BoardGameRental.Core;
namespace BoardGameRental.Tests;
internal static partial class Program
{
    private static void A7LookupPresentAbsent()
    {
        Check(CatalogLookup.Find(Sample(), 2) is Option<BoardGame>.Some found && found.Value.Name == "Uno", "Existing game not found.");
        Check(CatalogLookup.Find(Sample(), 999) is Option<BoardGame>.None, "Missing game fabricated.");
    }
    private static void A7PrimitiveValuesPresent()
    {
        Check(CatalogLookup.FindDailyPrice(Sample(), 4) is Option<decimal>.Some zero && zero.Value == 0m, "Free price mistaken for absence.");
        Check(CatalogLookup.FindDailyPrice(Sample(), 999) is Option<decimal>.None, "Absent price fabricated.");
        Check(new Option<bool>.Some(false).ToResult(new AppError("Missing", "Flag absent")) is Result<bool>.Success flag && !flag.Value, "False mistaken for absence.");
        Check(new Option<string>.Some("").ToResult(new AppError("Missing", "Text absent")) is Result<string>.Success text && text.Value == "", "Empty string mistaken for absence.");
    }
    private static void A7MissingBecomesFailure()
    {
        var error = new AppError("GameNotFound", "Game 999 was not found.");
        var absent = CatalogLookup.Find(Sample(), 999).ToResult(error);
        A7ErrorIs(absent, "GameNotFound");
        Check(absent is Result<BoardGame>.Failure bad && ReferenceEquals(bad.Error, error), "Missing error lost.");
        Check(CatalogLookup.Find(Sample(), 2).ToResult(error) is Result<BoardGame>.Success ok && ok.Value.Id == 2, "Present game rejected.");
    }
    private static void A7TariffLookupSuccess()
    {
        Check(RentalTariffOutcomes.Parse(" STANDARD ") is Result<RentalTariff>.Success standard && ReferenceEquals(standard.Value, RentalTariff.Standard), "Standard normalization failed.");
        Check(RentalTariffOutcomes.Parse("Student") is Result<RentalTariff>.Success student && student.Value.Calculate(Sample()[1], 3, 1) == 1200m, "Student result incorrect.");
    }
    private static void A7TariffLookupFailure()
    {
        foreach (string? name in new string?[] { null, "", "both", "vip" })
            A7ErrorIs(RentalTariffOutcomes.Parse(name), "UnknownTariff");
        Check(RentalTariffOutcomes.Parse("vip") is Result<RentalTariff>.Failure bad && bad.Error.Message.Contains("vip"), "Unsupported tariff not identified.");
    }
}
