namespace BoardGameRental.Core;

public static class RentalTariffOutcomes
{
    public static Result<RentalTariff> Parse(string? name) =>
        RentalTariff.TryCreate(name, out var tariff) && tariff is RentalTariff selected
            ? new Result<RentalTariff>.Success(selected)
            : new Result<RentalTariff>.Failure(new AppError("UnknownTariff",
                $"Tariff '{name ?? "<missing>"}' is unsupported; use standard or student."));
}
