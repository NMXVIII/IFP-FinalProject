namespace BoardGameRental.Core;

public sealed record AppError(string Code, string Message);

// No shared Value/Error properties: only the matching case exposes its payload.
public abstract class Result<T> where T : notnull
{
    private Result() { }
    public sealed class Success : Result<T>
    {
        public T Value { get; }
        public Success(T value) { ArgumentNullException.ThrowIfNull(value); Value = value; }
    }
    public sealed class Failure : Result<T>
    {
        public AppError Error { get; }
        public Failure(AppError error) { ArgumentNullException.ThrowIfNull(error); Error = error; }
    }
    public Result<U> Map<U>(Func<T, U> transform) where U : notnull => this switch
    {
        Success ok => new Result<U>.Success(transform(ok.Value)),
        Failure bad => new Result<U>.Failure(bad.Error),
        _ => throw new InvalidOperationException("Unknown Result case.")
    };
    public Result<U> Bind<U>(Func<T, Result<U>> next) where U : notnull => this switch
    {
        Success ok => next(ok.Value) ?? throw new InvalidOperationException("Bind returned null."),
        Failure bad => new Result<U>.Failure(bad.Error),
        _ => throw new InvalidOperationException("Unknown Result case.")
    };
    public U Match<U>(Func<T, U> success, Func<AppError, U> failure) => this switch
    {
        Success ok => success(ok.Value),
        Failure bad => failure(bad.Error),
        _ => throw new InvalidOperationException("Unknown Result case.")
    };
}
