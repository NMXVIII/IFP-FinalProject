namespace BoardGameRental.Core;

public static class PricingPolicies
{
    public static decimal Standard(BoardGame game, int days)
    {
        return game.DailyPrice * days;
    }

    public static decimal Student(BoardGame game, int days)
    {
        return decimal.Round(game.DailyPrice * days * 0.80m, 2,
            MidpointRounding.AwayFromZero);
    }
}
