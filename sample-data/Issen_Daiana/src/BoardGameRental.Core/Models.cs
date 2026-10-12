namespace BoardGameRental.Core;

// Nullable raw fields preserve the distinction between missing input and zero.
public sealed record RawGame(
    int? Id,
    string? Name,
    string? Category,
    decimal? DailyPrice,
    int? AvailableCopies);

public sealed record BoardGame(
    int Id,
    string Name,
    string Category,
    decimal DailyPrice,
    int AvailableCopies);

public sealed record RentalRequest(int Days, decimal Budget);

public sealed record RentalQuote(
    int GameId,
    string GameName,
    string Category,
    int Days,
    decimal TotalPrice);
