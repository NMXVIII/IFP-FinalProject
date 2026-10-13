using System.Text.Json;

namespace BoardGameRental.Core;

// Application boundary: file access and parsing are outside the pure pipeline.
public static class CatalogFile
{
    public static bool TryLoad(
        string path,
        out IReadOnlyList<BoardGame> games,
        out IReadOnlyList<string> errors)
    {
        games = Array.Empty<BoardGame>();
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            RawGame?[]? rawGames = JsonSerializer.Deserialize<RawGame?[]>(json, options);
            return InputValidation.TryValidateCatalog(rawGames, out games, out errors);
        }
        catch (JsonException)
        {
            errors = new[] { $"Invalid JSON or incorrect field type in '{path}'." };
        }
        catch (IOException exception)
        {
            errors = new[] { $"Cannot read '{path}': {exception.Message}" };
        }
        catch (UnauthorizedAccessException)
        {
            errors = new[] { $"Access denied to '{path}'." };
        }
        catch (ArgumentException)
        {
            errors = new[] { "The file path is invalid." };
        }
        catch (NotSupportedException)
        {
            errors = new[] { "The file path format is not supported." };
        }
        return false;
    }
}
