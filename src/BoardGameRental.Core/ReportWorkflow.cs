namespace BoardGameRental.Core;

public interface IStockPlanner
{
    Result<int> Needed(int availableCopies, int targetCopies);
}

public static class ReportWorkflow
{
    public static Result<ReportBundle> Build(
        IReadOnlyList<BoardGame> games, ReportRequest request, IStockPlanner planner) =>
        ReportInput.Validate(games).Bind(snapshot => BuildValidated(snapshot, request, planner));

    private static Result<ReportBundle> BuildValidated(
        IReadOnlyList<BoardGame> snapshot, ReportRequest request, IStockPlanner planner)
    {
        var stock = new List<StockAssessment>(snapshot.Count);
        foreach (BoardGame game in snapshot)
        {
            Result<int> outcome = planner.Needed(game.AvailableCopies, request.TargetCopies);
            if (outcome is Result<int>.Failure failure)
                return new Result<ReportBundle>.Failure(failure.Error);
            if (outcome is Result<int>.Success success)
                stock.Add(new StockAssessment(game, success.Value));
        }

        Func<BoardGame, decimal> standard = ReportPricing.CreatePrice(RentalTariff.Standard)(request.Days);
        Func<BoardGame, decimal> student = ReportPricing.CreatePrice(RentalTariff.Student)(request.Days);

        // Materialise once before display/export: no deferred native calls or file reads.
        var reports = new ReportBundle(request.Days, request.Budget, request.TargetCopies,
            Array.AsReadOnly(RentalReports.AffordableRentals(snapshot, request.Days, request.Budget, standard).ToArray()),
            Array.AsReadOnly(RentalReports.AffordableRentals(snapshot, request.Days, request.Budget, student).ToArray()),
            CategoryReports.Inventory(stock));
        return new Result<ReportBundle>.Success(reports);
    }

    public static Result<ReportBundle> Run(
        string inputPath, string outputPath, ReportRequest request, IStockPlanner planner)
    {
        // An export may not overwrite its source catalog, even via a relative path.
        try
        {
            if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase))
                return new Result<ReportBundle>.Failure(new AppError(
                    "ExportOverwritesInput", "Choose an export path different from the input catalog."));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        {
            return new Result<ReportBundle>.Failure(new AppError("InvalidPath", ex.Message));
        }

        return CatalogBoundary.Load(inputPath)
            .Bind(games => Build(games, request, planner))
            .Bind(reports => ReportExport.Save(outputPath, reports).Map(_ => reports));
    }
}
