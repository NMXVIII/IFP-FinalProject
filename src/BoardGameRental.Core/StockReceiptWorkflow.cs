namespace BoardGameRental.Core;

public sealed record StockReceipt(BoardGame UpdatedGame, IReadOnlyList<BoardGame> Catalog);

public static class StockReceiptWorkflow
{
    public static Result<StockReceipt> Receive(IReadOnlyList<BoardGame> catalog, int gameId, int copies)
    {
        if (copies < 1)
            return Fail("InvalidStockReceipt", "Received copies must be positive.");

        return CatalogLookup.Find(catalog, gameId)
            .ToResult(new AppError("GameNotFound", $"Game {gameId} was not found in the catalog."))
            .Bind(game =>
            {
                if (copies > int.MaxValue - game.AvailableCopies)
                    return Fail("StockOverflow", "The updated stock count exceeds the supported integer range.");

                BoardGame updated = game with { AvailableCopies = game.AvailableCopies + copies };
                var snapshot = Array.AsReadOnly(catalog.Select(item => item.Id == gameId ? updated : item).ToArray());
                return new Result<StockReceipt>.Success(new StockReceipt(updated, snapshot));
            });
    }

    public static Result<StockReceipt> Run(string input, int gameId, int copies, string output) =>
        CatalogBoundary.Load(input)
            .Bind(catalog => Receive(catalog, gameId, copies))
            .Bind(receipt => ReportExport.SaveSnapshot(output, receipt.Catalog, "CatalogSaveFailed", overwrite: false)
                .Map(_ => receipt));

    private static Result<StockReceipt> Fail(string code, string message) =>
        new Result<StockReceipt>.Failure(new AppError(code, message));
}
