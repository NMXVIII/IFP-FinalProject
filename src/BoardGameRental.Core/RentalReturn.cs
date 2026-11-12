namespace BoardGameRental.Core;

public sealed record ReturnReceipt(string GameName, int Quantity, int ActualDays,
    int OverdueDays, decimal LateCharge, decimal FinalCharge);

public static class RentalReturn
{
    public static Result<ReturnReceipt> Settle(RentalState state, int actualDays) => state switch
    {
        RentalState.Confirmed confirmed => SettleConfirmed(confirmed.Rental, actualDays),
        _ => new Result<ReturnReceipt>.Failure(new AppError("InvalidState", "Only confirmed rentals can be settled."))
    };

    private static Result<ReturnReceipt> SettleConfirmed(ValidatedRental rental, int actualDays)
    {
        if (actualDays is < 1 or > 365)
            return new Result<ReturnReceipt>.Failure(new AppError("InvalidReturnDays", "Actual days must be from 1 to 365."));

        int overdueDays = Math.Max(0, actualDays - rental.Days);
        return RentalTariffOutcomes.Parse(rental.TariffName).Map(tariff =>
        {
            decimal lateCharge = tariff.Calculate(rental.Game, overdueDays, rental.Quantity);
            return new ReturnReceipt(rental.Game.Name, rental.Quantity, actualDays,
                overdueDays, lateCharge, rental.TotalPrice + lateCharge);
        });
    }
}
