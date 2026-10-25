namespace BoardGameRental.Core;

public abstract class Option<T> where T : notnull
{
    private Option() { }
    public sealed class Some : Option<T>
    {
        public T Value { get; }
        public Some(T value) { ArgumentNullException.ThrowIfNull(value); Value = value; }
    }
    public sealed class None : Option<T> { }
    public Result<T> ToResult(AppError missing) => this switch
    {
        Some found => new Result<T>.Success(found.Value),
        None => new Result<T>.Failure(missing),
        _ => throw new InvalidOperationException("Unknown Option case.")
    };
}

public static class CatalogLookup
{
    public static Option<BoardGame> Find(IReadOnlyList<BoardGame> games, int id)
    {
        foreach (BoardGame game in games)
            if (game.Id == id) return new Option<BoardGame>.Some(game);
        return new Option<BoardGame>.None();
    }
    // Real optional primitive lookup: a free game's price is Some(0), never None.
    public static Option<decimal> FindDailyPrice(IReadOnlyList<BoardGame> games, int id) =>
        Find(games, id) is Option<BoardGame>.Some found
            ? new Option<decimal>.Some(found.Value.DailyPrice) : new Option<decimal>.None();
}
