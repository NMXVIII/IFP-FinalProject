using System.Text.Json;

namespace BoardGameRental.Core;

public static class ReportExport
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Ownership of destination is transferred to this method, including on failure.
    public static Result<bool> WriteAndClose(Stream destination, ReportBundle reports)
    {
        try
        {
            using (destination)
            {
                JsonSerializer.Serialize(destination, reports, JsonOptions);
                destination.Flush();
            }
            return new Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new Result<bool>.Failure(new AppError("ExportFailed", ex.Message));
        }
    }

    public static Result<string> Save(string path, ReportBundle reports)
    {
        string? temporary = null;
        try
        {
            string fullPath = Path.GetFullPath(path);
            // Same directory/volume: publish only after successful serialization and disposal.
            temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            Result<bool> written = WriteAndClose(stream, reports);
            if (written is Result<bool>.Failure failure)
                return new Result<string>.Failure(new AppError(
                    failure.Error.Code, $"Export '{path}': {failure.Error.Message}"));
            File.Move(temporary, fullPath, overwrite: true);
            return new Result<string>.Success(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new Result<string>.Failure(new AppError("ExportFailed", $"Export '{path}': {ex.Message}"));
        }
        finally
        {
            if (temporary is not null)
            {
                try { File.Delete(temporary); }
                catch (IOException) { /* Preserve the original outcome if external locking prevents cleanup. */ }
                catch (UnauthorizedAccessException) { /* The original error remains the useful boundary outcome. */ }
            }
        }
    }
}
