using System.Text.Json;
namespace BoardGameRental.Core;

// Only this boundary performs I/O for the Assignment 7 chain. No state is modified.
public static class CatalogBoundary
{
    public static Result<IReadOnlyList<BoardGame>> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<RawGame?[]>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return InputValidation.TryValidateCatalog(raw, out var games, out var errors)
                ? new Result<IReadOnlyList<BoardGame>>.Success(games)
                : Fail("InvalidCatalog", $"Catalog '{path}': {string.Join("; ", errors)}");
        }
        catch (FileNotFoundException ex) { return Fail("FileMissing", $"Load '{path}': {ex.Message}"); }
        catch (DirectoryNotFoundException ex) { return Fail("FileMissing", $"Load '{path}': {ex.Message}"); }
        catch (JsonException ex) { return Fail("MalformedJson", $"Parse '{path}': {ex.Message}"); }
        catch (UnauthorizedAccessException ex) { return Fail("AccessDenied", $"Load '{path}': {ex.Message}"); }
        catch (IOException ex) { return Fail("FileReadFailed", $"Load '{path}': {ex.Message}"); }
        catch (ArgumentException ex) { return Fail("InvalidPath", $"Load '{path}': {ex.Message}"); }
        catch (NotSupportedException ex) { return Fail("InvalidPath", $"Load '{path}': {ex.Message}"); }
    }
    private static Result<IReadOnlyList<BoardGame>> Fail(string code, string message) =>
        new Result<IReadOnlyList<BoardGame>>.Failure(new AppError(code, message));
}
