namespace BoardGameRental.Core;

public sealed record ExtensionReceipt(RentalState.Confirmed Rental, int AdditionalDays, decimal AdditionalCharge);

public static class RentalExtension
{
    public static Result<ExtensionReceipt> Extend(RentalState state, int additionalDays, decimal totalBudget) => state switch
    {
        RentalState.Confirmed confirmed => ExtendConfirmed(confirmed.Rental, additionalDays, totalBudget),
        _ => new Result<ExtensionReceipt>.Failure(new AppError("InvalidState", "Only confirmed rentals can be extended."))
    };

    private static Result<ExtensionReceipt> ExtendConfirmed(ValidatedRental original,
        int additionalDays, decimal totalBudget)
    {
        if (additionalDays < 1 || additionalDays > 30 - original.Days)
            return new Result<ExtensionReceipt>.Failure(new AppError("InvalidExtensionDays",
                "Add at least one day without exceeding 30 total rental days."));

        return RentalTariffOutcomes.Parse(original.TariffName)
            .Bind(tariff => ValidatedRentalOutcomes.Create(new RentalDraft(original.Game,
                original.Days + additionalDays, original.Quantity, totalBudget, tariff)))
            .Map(rental => new ExtensionReceipt(new RentalState.Confirmed(rental),
                additionalDays, rental.TotalPrice - original.TotalPrice));
    }
}
