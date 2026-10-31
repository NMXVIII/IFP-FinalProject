namespace BoardGameRental.Core;

public sealed class ReportRequest
{
    public int Days { get; }
    public decimal Budget { get; }
    public int TargetCopies { get; }

    private ReportRequest(int days, decimal budget, int targetCopies)
    {
        Days = days;
        Budget = budget;
        TargetCopies = targetCopies;
    }

    public static Result<ReportRequest> Create(int days, decimal budget, int targetCopies)
    {
        if (days is < 1 or > 30 || budget is < 0 or > 1000000000m ||
            decimal.Round(budget, 2) != budget || targetCopies < 0)
        {
            return new Result<ReportRequest>.Failure(new AppError("InvalidReportRequest",
                "Days: 1..30; budget: 0..1000000000 KZT with at most 2 decimal places; target copies: 0..2147483647."));
        }
        return new Result<ReportRequest>.Success(new ReportRequest(days, budget, targetCopies));
    }
}

public static class ReportInput
{
    // Reuse the earlier domain rules and take an independent validated snapshot.
    public static Result<IReadOnlyList<BoardGame>> Validate(IReadOnlyList<BoardGame>? games)
    {
        RawGame?[]? raw = games?.Select(game => game is null ? null : new RawGame(
            game.Id, game.Name, game.Category, game.DailyPrice, game.AvailableCopies)).ToArray();
        return InputValidation.TryValidateCatalog(raw, out var validated, out var errors)
            ? new Result<IReadOnlyList<BoardGame>>.Success(validated)
            : new Result<IReadOnlyList<BoardGame>>.Failure(new AppError(
                "InvalidCatalog", string.Join("; ", errors)));
    }
}
