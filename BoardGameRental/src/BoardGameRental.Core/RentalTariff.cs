namespace BoardGameRental.Core;

public sealed class RentalTariff
{
    public string Name { get; }
    private readonly Func<BoardGame, int, decimal> calculate;

    private RentalTariff(string name, Func<BoardGame, int, decimal> calculate)
    {
        Name = name;
        this.calculate = calculate;
    }

    public static RentalTariff Standard { get; } = new("standard", PricingPolicies.Standard);
    public static RentalTariff Student { get; } = new("student", PricingPolicies.Student);

    public static bool TryCreate(string? name, out RentalTariff? tariff)
    {
        tariff = name?.Trim().ToLowerInvariant() switch
        {
            "standard" => Standard,
            "student" => Student,
            _ => null
        };
        return tariff is not null;
    }

    public decimal Calculate(BoardGame game, int days, int quantity)
    {
        return calculate(game, days) * quantity;
    }
}
