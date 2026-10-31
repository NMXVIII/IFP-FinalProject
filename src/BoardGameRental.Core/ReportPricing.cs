namespace BoardGameRental.Core;

public static class ReportPricing
{
    // Tariff -> (Days -> (Game -> Price)); each call fixes one argument.
    public static Func<int, Func<BoardGame, decimal>> CreatePrice(RentalTariff tariff) =>
        days => game => tariff.Calculate(game, days, 1);
}
