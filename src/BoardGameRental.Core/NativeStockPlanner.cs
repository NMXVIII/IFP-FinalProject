using System.Runtime.InteropServices;

namespace BoardGameRental.Core;

public sealed class NativeStockPlanner : IStockPlanner
{
    private readonly Func<int, int, int> invoke;

    public NativeStockPlanner() : this(RestockNeeded) { }

    // Injection tests binding failures without depending on the process DLL cache.
    public NativeStockPlanner(Func<int, int, int> invoke) => this.invoke = invoke;

    [DllImport("boardgame_native", EntryPoint = "brg_restock_needed",
        CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    private static extern int RestockNeeded(int availableCopies, int targetCopies);

    public Result<int> Needed(int availableCopies, int targetCopies)
    {
        if (availableCopies < 0 || targetCopies < 0)
            return Fail("InvalidStock", "Available and target copies must be non-negative Int32 counts.");
        try
        {
            int value = invoke(availableCopies, targetCopies);
            return value >= 0 && value <= targetCopies
                ? new Result<int>.Success(value)
                : Fail("NativeCallFailed", "brg_restock_needed returned an invalid count.");
        }
        catch (DllNotFoundException ex)
        {
            return Fail("NativeLibraryMissing", $"Load the boardgame_native library (boardgame_native.dll, libboardgame_native.dylib or libboardgame_native.so) beside the application; run native/build.sh on macOS/Linux: {ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            return Fail("NativeEntryPointMissing", $"Find the export brg_restock_needed in the boardgame_native library: {ex.Message}");
        }
        catch (BadImageFormatException ex)
        {
            return Fail("NativeArchitectureMismatch", $"Use a native library built for this OS and CPU architecture: {ex.Message}");
        }
    }

    private static Result<int> Fail(string code, string message) =>
        new Result<int>.Failure(new AppError(code, message));
}
