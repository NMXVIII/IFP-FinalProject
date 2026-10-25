using System.Globalization;

namespace BoardGameRental.Core;

public static class InputValidation
{
    public static bool TryValidateCatalog(
        IReadOnlyList<RawGame?>? rawGames,
        out IReadOnlyList<BoardGame> games,
        out IReadOnlyList<string> errors)
    {
        games = Array.Empty<BoardGame>();
        var messages = new List<string>();
        var validated = new List<BoardGame>();
        var ids = new HashSet<int>();

        if (rawGames is null)
        {
            errors = new[] { "The catalog is missing. Use [] for a valid empty catalog." };
            return false;
        }

        for (int index = 0; index < rawGames.Count; index++)
        {
            RawGame? raw = rawGames[index];
            string label = $"Record {index + 1}";
            if (raw is null)
            {
                messages.Add($"{label}: the record is missing.");
                continue;
            }

            int previousErrors = messages.Count;
            if (raw.Id is not int id || id <= 0)
                messages.Add($"{label}: Id must be a positive integer.");
            else if (!ids.Add(id))
                messages.Add($"{label}: duplicate Id {id}.");

            if (string.IsNullOrWhiteSpace(raw.Name))
                messages.Add($"{label}: Name is required.");
            if (string.IsNullOrWhiteSpace(raw.Category))
                messages.Add($"{label}: Category is required.");

            if (raw.DailyPrice is not decimal price || price < 0 || price > 1000000)
                messages.Add($"{label}: DailyPrice must be between 0 and 1000000 KZT.");
            else if (decimal.Round(price, 2) != price)
                messages.Add($"{label}: DailyPrice must have at most two decimal places.");

            if (raw.AvailableCopies is not int copies || copies < 0)
                messages.Add($"{label}: AvailableCopies must be a non-negative integer.");

            if (messages.Count == previousErrors &&
                raw.Id is int validId && raw.Name is string name &&
                raw.Category is string category && raw.DailyPrice is decimal validPrice &&
                raw.AvailableCopies is int validCopies)
            {
                validated.Add(new BoardGame(validId, name.Trim(), category.Trim(),
                    validPrice, validCopies));
            }
        }

        errors = Array.AsReadOnly(messages.ToArray());
        if (messages.Count > 0)
            return false;

        games = Array.AsReadOnly(validated.ToArray());
        return true;
    }

    public static bool TryParseRequest(
        string? daysText,
        string? budgetText,
        out RentalRequest? request,
        out string error)
    {
        request = null;
        if (!int.TryParse(daysText, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int days) || days < 1 || days > 30)
        {
            error = "Days is required and must be an integer from 1 to 30.";
            return false;
        }

        if (!decimal.TryParse(budgetText, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out decimal budget) ||
            budget < 0 || budget > 1000000000 || decimal.Round(budget, 2) != budget)
        {
            error = "Budget is required: 0 to 1000000000 KZT, at most two decimal places (use a dot).";
            return false;
        }

        request = new RentalRequest(days, budget);
        error = string.Empty;
        return true;
    }
}
