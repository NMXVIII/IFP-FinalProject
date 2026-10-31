# BoardGameRental - Assignment 8: Extension Methods and LINQ

An integrated C# console project for a board-game rental shop. Assignments 5-7
remain runnable: recommendations, validated rental preparation/confirmation,
Option lookups, Result composition and safe JSON loading. Assignment 8 adds
affordable-rental and inventory reports, configurable prices, deferred queries,
safe JSON export and a real native C restocking calculation.

## Team and responsibilities

All members are in group **IT-2511**. Each member has implementation, assertions
and documentation responsibilities in the shared project.

| Member | Assignment 8 implementation | Automated tests | Documentation responsibility |
|---|---|---|---|
| Issen Daiana | `ReportModels.cs`, `ReportExtensions.cs`, `RentalReports.cs`: report records, two extensions, affordable report and `yield return` | Eight cases in `ReportQueryTests.cs` | Report 1, receiver contracts, query operators and deferred timing |
| Kabdrakhmanova Zarina | `ReportPricing.cs`, `CategoryReports.cs`, `ReportRequest.cs`: curried configuration, category totals, validated input | Eight cases in `ReportConfigurationTests.cs` | Report 2, price examples, validation rules and deterministic ordering |
| Kurmashev Damir | `ReportWorkflow.cs`, `ReportExport.cs`: Result workflow, snapshots, protected source and safe export | Eight cases in `ReportWorkflowTests.cs` | Workflow, resource ownership, materialisation and retention note |
| Nurlanbek Marghulan | `native/`, `NativeStockPlanner.cs`, `ReportsConsole.cs`, application routing and DLL copy settings | Eight cases in `NativeReportTests.cs` | Native contract, compiler/build/loading procedure, CLI and integration evidence |

New core files are in `src/BoardGameRental.Core/`; test files are in
`tests/BoardGameRental.Tests/`. Console files are in `src/BoardGameRental.App/`.
Earlier team contributions remain in the Assignment 5/6 documentation and the
Assignment 7 modules listed below.

## Prerequisites and clean build

Supported Assignment 8 platforms: **Windows x64** (tested on Windows 11 x64,
version 10.0.26300) and **macOS arm64** (Apple M1; native library built with
Apple clang, see the macOS commands below). Tested SDK: **.NET 8 SDK 8.0.425**.
`global.json` selects a compatible installed .NET 8 feature band. There are no
external NuGet packages and no network service, database, API key or secret is needed.

Extract the full project and open a PowerShell terminal in the directory containing
`BoardGameRental.sln`, `src`, `tests`, `native` and `runtimes`. All commands below
use that directory. The included DLL permits a clean .NET build without installing
a C compiler. Rebuilding the C source requires the compiler described below.

macOS (Terminal, Apple Silicon): build the native library first, then use the same
dotnet commands.

```bash
bash native/build.sh
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo8
```

Windows (PowerShell):

```powershell
dotnet --version
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo8
dotnet run --project src/BoardGameRental.App -- --reports8
```

Expected: build succeeds with 0 warnings/errors; **87/87 passed; 0 failed.**
The tests are an assertions-based executable, so use `dotnet run` above. It exits
0 only when all assertions pass. `--demo8` runs a real successful native workflow
and expected missing/malformed-file failures in a temporary directory, then removes
its files; exit 0 means the demonstration behaved as expected.
`--reports8` displays both reports and saves `reports.json` in the working directory.

Optional Windows launchers: `test.bat` runs the assertions; `demo8.bat` runs the new
demonstration; `run.bat` retains the Assignment 5 entry point and `demo7.bat` retains
the Assignment 7 demonstration. The existing `.vscode/settings.json` supports
launching these batch files through Code Runner from a PowerShell terminal.

## Inputs and domain rules

`sample-data/games.json` is the actual catalog. Each record has a positive unique
integer `Id`, nonblank `Name` and `Category`, `DailyPrice` from 0 to 1,000,000 KZT
with at most two decimal places, and non-negative `AvailableCopies` (Int32).
Validation trims text. Categories are grouped and sorted with ordinal,
case-sensitive comparison. A zero price is a legitimate free game; zero stock
means unavailable for rental but remains relevant to inventory planning.

Reports require 1-30 days, a budget from 0 to 1,000,000,000 KZT with at most two
decimal places, and a target stock count from 0 to 2,147,483,647 per title.
The report is for **one copy** of each title. Standard price is daily price times
days; student price applies a 20% discount. Both use the existing tariff policies,
with two-decimal midpoint rounding away from zero. Exact-budget offers are included.
Daily inventory value is `DailyPrice * AvailableCopies`, not an asset purchase value.
Native restocking counts are integer copies and involve no currency rounding.

Report request construction returns `InvalidReportRequest` on invalid settings.
`CatalogBoundary.Load` and `ReportInput.Validate` reject the complete invalid catalog
as `InvalidCatalog`; no partial data is used. Direct query functions expect validated
records and pure price functions. `ReportWorkflow.Build` validates its snapshot
before querying or invoking the adapter.

## Report 1: affordable rentals

**Intended user:** staff helping a customer choose a rental within a budget.
**Input:** validated catalog, duration, budget and a configured pricing function.
**Filters:** available copies > 0, then total price <= budget.
**Fields:** game ID, name, category, duration and total KZT.
**Order:** total price ascending, then game ID ascending for tied prices.

`RentalReports.AffordableRentals` composes `AvailableForRental` (`Where`), `Select`
with `QuoteFor`, another `Where`, `OrderBy` and `ThenBy`, and yields report values.
The application shows this report under both configured tariffs.

Expected sample for 3 days and a 3,000 KZT budget:

| Tariff | Ordered games and totals (KZT) |
|---|---|
| Standard | Chess (4): 0.00; Uno (2): 1500.00; Monopoly (1): 3000.00 |
| Student | Chess (4): 0.00; Uno (2): 1200.00; Monopoly (1): 2400.00; Carcassonne (6): 3000.00 |

Empty input and no matching offers produce a valid empty list.

### Custom extension contracts

| Extension | Receiver and return type | Behaviour and reason for this receiver |
|---|---|---|
| `AvailableForRental()` | `IEnumerable<BoardGame>` -> `IEnumerable<BoardGame>` | Deferred stock eligibility for a collection; excludes unavailable titles without discarding free games or changing the source |
| `QuoteFor(days, configuredPrice)` | `BoardGame` -> `RentalOffer` | Projects that game's identity/category and a supplied one-copy calculation into a report row; one game owns the fields needed for its offer |

Both are used by the application through the report workflow, and both have direct
assertions. They perform no file access, display or shared-state updates.

### Currying and partial application

`ReportPricing.CreatePrice` has the shape
`RentalTariff -> (int Days -> (BoardGame -> decimal Price))`.

```csharp
Func<BoardGame, decimal> standard = ReportPricing.CreatePrice(RentalTariff.Standard)(3);
Func<BoardGame, decimal> student = ReportPricing.CreatePrice(RentalTariff.Student)(3);
```

The workflow passes these two functions to the same query and dataset. For Uno,
standard gives 1500.00 KZT while student gives 1200.00 KZT. The student configuration
also admits Carcassonne at exactly 3000.00 KZT. Each closure captures only an
immutable tariff and duration; switching configuration does not mutate global state.

## Report 2: category inventory and restocking

**Intended user:** the shop manager planning stock purchases.
**Input:** the same full catalog plus one native restocking result for each title.
**Filters:** no stock filter; sold-out and free titles must remain visible.
**Fields:** category, title count, total available copies, daily stock value in KZT,
and copies to buy to reach the requested target for each title.
`CategoryReports.Inventory` uses `GroupBy`, `Select`, `Count`, `Sum`,
`OrderByDescending` on daily stock value and an ordinal `ThenBy` on category.
Counts of copies are summed as Int64 to avoid Int32 aggregation overflow.

Expected sample at target 2 copies per title:

| Category | Titles | Available copies | Daily stock value (KZT) | Copies to buy from C |
|---|---:|---:|---:|---:|
| Strategy | 3 | 3 | 4000.00 | 3 |
| Family | 1 | 2 | 2000.00 | 0 |
| Card | 1 | 3 | 1500.00 | 0 |
| Classic | 1 | 1 | 0.00 | 1 |

Total daily stock value: 7500.00 KZT. Total copies to buy: 4.
Tied values use category order; empty input yields no category rows.

## Native C contract and reproducible build

Source and export: `native/boardgame_native.c`, `native/boardgame_native.h`.

```c
int32_t __cdecl brg_restock_needed(int32_t available_copies, int32_t target_copies);
```

On Windows the export uses `__cdecl` and `__declspec(dllexport)`; on macOS/Linux the
macros are empty and the default C calling convention is used (`Cdecl` in C#).
Both inputs are counts from 0 through `INT32_MAX`. Output is
`max(0, target_copies - available_copies)`; a negative input returns -1.
The subtraction occurs only when target > available, so valid inputs cannot
overflow. Manual example: available 1, target 2 -> 1 copy to buy. Available 3,
target 2 -> 0. The function allocates no memory and returns no handle.

Compiler used: **MSYS2 UCRT64 GCC 16.1.0, Rev5**, target
`x86_64-w64-mingw32`. Windows provides the Universal C Runtime; the DLL does not
depend on MSYS2's shell or a separately distributed GCC runtime DLL.
Install the MSYS2 UCRT64 `mingw-w64-ucrt-x86_64-gcc` package if rebuilding is needed.
The script discovers `gcc` on PATH, then checks `C:\msys64\ucrt64\bin\gcc.exe`.

From the project root in PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\native\build.ps1
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo8
```

Or pass a compiler path to the build script with `-Compiler`.
The exact compiler command, run from the project root, is:

```powershell
gcc -std=c11 -O2 -Wall -Wextra -Werror -shared -o runtimes/win-x64/native/boardgame_native.dll native/boardgame_native.c
```

The included Windows binary is `runtimes/win-x64/native/boardgame_native.dll`.

macOS arm64 build (compiler: Apple clang, see `clang --version`; OS: macOS; arch: arm64).
From the project root, `bash native/build.sh` runs the exact command

```bash
clang -std=c11 -O2 -Wall -Wextra -Werror -dynamiclib -arch arm64 -o runtimes/osx-arm64/native/libboardgame_native.dylib native/boardgame_native.c
```

and writes `runtimes/osx-arm64/native/libboardgame_native.dylib`. On Linux x64 the
script builds `libboardgame_native.so` with `-shared -fPIC`.
App/test project files copy the library for the building OS beside the managed
assemblies during build/publish (MSBuild `IsOSPlatform` conditions); the binding name
`boardgame_native` resolves to `boardgame_native.dll`, `libboardgame_native.dylib` or
`libboardgame_native.so`. No `PlatformTarget` or `unsafe` setting is required. If the
library is missing, the adapter returns `NativeLibraryMissing` instead of crashing.
`NativeStockPlanner` binds the exact export with `DllImport`, Int32 parameters and
return type, `CallingConvention.Cdecl`, and assembly-directory search.
It validates inputs before invocation and maps missing DLL, missing export,
incompatible architecture and invalid return values to explicit Result failures.

`ReportWorkflow` invokes the real adapter once per validated title, supplies the
returned values as `StockAssessment` records to the pure category query, and exports
those values. Production has no managed replacement for the native calculation.

Integration difficulty 2 (macOS): the Windows-only DLL and x64 `PlatformTarget` do
not load on Apple Silicon. Resolution: a `build.sh` producing an arm64 dylib, OS-conditional
copy items in the project files, and no fixed `PlatformTarget`.

Integration difficulty 1 (Windows): GCC failed when an absolute output argument
contained Cyrillic parent-directory characters. `native/build.ps1` now changes to
the project root, passes relative source/output arguments, and restores the caller's
directory in `finally`. Rebuilding and the real P/Invoke call were verified from
the Cyrillic workspace path.

`A8NativeRealCall` asserts the manual examples against the actual DLL, including
integer bounds. Adapter failure tests inject throwing native delegates; this avoids
confusing an already-loaded DLL with a fresh load. They supplement the real-call
test. An isolated fresh-process missing-DLL check was also performed during packaging.
Official references: [P/Invoke](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
and [native library loading](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading).

## Deferred execution, ownership and retention

`RentalReports.AffordableRentals` is an `IEnumerable<RentalOffer>` iterator with
`yield return`. Creating it, or obtaining its enumerator, does not read its input
or run the pricing function. Iteration triggers processing. Sorting must consume
the input before producing the first sorted row. Test-local source/pricing probes
show 0/0 work before iteration, then 6 input records and 5 prices on the sample.
No diagnostic counters occur in production calculations.

Re-enumerating a deferred query repeats filtering, pricing and sorting and can see
changes in a caller-owned source. `ReportWorkflow` instead validates an independent
catalog snapshot, calls the adapter eagerly, and deliberately uses `ToArray` plus
read-only wrappers before display/export. Reusing these report lists repeats no
file reads, price calculation or native calls. Empty results are materialised too.

Resource owners and release points:

| Resource | Owner and release |
|---|---|
| Input file | `CatalogBoundary.Load` uses `File.ReadAllText`, which owns and closes its internal handle before returning/throwing; JSON parsing is performed on the resulting string |
| Export stream | `ReportExport.Save` creates a FileStream and immediately transfers ownership to `WriteAndClose`; its `using` releases the stream on success, write failure or flush failure |
| Export temporary file | `Save` writes a GUID-named file in the destination directory, closes it, then replaces the destination; `finally` removes the temporary file on handled failures. External locking/permissions can prevent cleanup, so cleanup is best-effort in that case |
| Iterator source | LINQ/`foreach` dispose source enumerators; an early-stop test checks a test-local source's `finally`. Production queries hold no open stream |
| Test/demo files | Each test/demo owns a unique temporary directory and removes it in `finally`; JsonDocument and explicit test streams use `using` |
| Native library | The CLR owns the P/Invoke module for the process lifetime. There is no returned native allocation/handle to release |

Input and existing export remain unchanged when loading, validation or native
processing fails. Export writes become visible only after successful serialization
and closure. A failed destination replacement preserves its existing file.
Using the source catalog as the export path is rejected as `ExportOverwritesInput`.

The JSON loader retains a complete input string and validated records during
loading. Validation makes a new snapshot, and reports keep only their result rows;
temporary raw arrays and stock-assessment lists become collectible after the build.
Deferred iterators retain their source and configured delegate until callers release
them. Configured price closures retain tariff and duration, not the dataset.
Sorting and grouping buffer data; snapshots use O(n) space for this small catalog.
Streaming could reduce retained input for a large sequential operation, but global
sorting/grouping and stable repeated display/export require materialisation here.
Materialising once avoids repeated query buffers and boundary calls. There are no
long-lived dataset collections, unbounded caches or event subscriptions. Static
tariffs and serializer options hold no catalog. No manual GC or native allocation
is used, and no memory-leak fix is claimed.

## Normal and failure commands

```powershell
# Real reports and persistent export; returns 0.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/games.json 3 3000 2 reports.json

# Empty input is valid; returns 0 with three empty result lists.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/empty.json 3 3000 2 reports.json

# Returns 1 with MalformedJson; does not replace an existing export.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/malformed.json 3 3000 2 reports.json

# Returns 1 with InvalidCatalog.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/invalid.json 3 3000 2 reports.json

# Returns 1 with InvalidReportRequest.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/games.json 0 3000 2 reports.json

# Returns 1 with ExportOverwritesInput; source is preserved.
dotnet run --project src/BoardGameRental.App -- --reports8 sample-data/games.json 3 3000 2 sample-data/games.json
```

Missing-file demonstration uses a controlled nonexistent path in `--demo8`.
A missing export directory produces `ExportFailed`; paths containing spaces must
be quoted. Domain/boundary failures are handled once by `Result.Match` in the console.
The demo intentionally prints expected failures but still exits 0 if all scenarios
behave correctly. Individual `--reports8` failures return 1.

## Earlier assignments remain runnable

```powershell
# Assignment 5: both pricing pipelines; expected IDs standard 4,2,1 / student 4,2,1,6.
dotnet run --project src/BoardGameRental.App

# Assignment 6: Draft -> Ready -> Confirmed, with guarded/idempotent confirmation.
dotnet run --project src/BoardGameRental.App -- --demo

# Assignment 7: explicit successes/failures, Option and file-boundary demonstrations.
dotnet run --project src/BoardGameRental.App -- --demo7
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 1 3000 student
```

The last command confirms Uno at 1200.00 KZT. Unknown ID, invalid quantity,
insufficient stock, unaffordable total, invalid tariff and invalid state remain
explicit failures. Rentals use immutable validated snapshots; reporting/export does
not reserve stock, charge a payment or mutate a rental state.

## Requirement and test map

| Requirement | Implementation | Assertion cases and expected outcomes |
|---|---|---|
| Task 1: used extensions | `ReportExtensions.AvailableForRental`, `QuoteFor`; called from `RentalReports` | `A8StockExtension`, `A8QuoteExtension`: correct eligibility/row, supplied function once, no mutation |
| Task 2: distinct LINQ reports | `RentalReports.AffordableRentals`, `CategoryReports.Inventory` | `A8AffordableNormal/Empty/NoMatches/Ties`, `A8CategoryTotals/Empty/Ties/Zeros`: sample tables, empty/no matches, deterministic ties and correct aggregation |
| Task 3: currying/configuration | `ReportPricing.CreatePrice`; configured in `ReportWorkflow` | `A8ConfiguredSameInput`, `A8ConfiguredRounding`: Uno 1500 vs 1200, different eligible lists, correct rounding |
| Task 4: deferred workflow | `RentalReports` iterator; workflow `ToArray` snapshot | `A8DeferredTiming`, `A8DeferredEarlyStop`, `A8MaterializedSnapshot`: no early work, processing on iteration, repeatable materialised reuse and source disposal |
| Task 5: disposal | `ReportExport.WriteAndClose/Save`, bounded `CatalogBoundary.Load` | `A8ExportSuccessDisposes`, `A8ExportFailureDisposes`, `A8ExportReplacesExisting`, `A8ExportFailurePreservesExisting`: disposed streams, reusable/deletable files, previous export preserved and no temp leftovers |
| Task 6: retention | Resource note above and read-only `ReportBundle` lists | `A8MaterializedSnapshot`, `A8WorkflowSnapshot`: reuse does not repeat prices/native calls or mutate source |
| Tasks 7-8: C library/P/Invoke | `native/`, runtime DLL, `NativeStockPlanner.Needed`, project copy settings | `A8NativeRealCall`: manual native result and extremes; `A8NativeInvalidInputs/LoadFailure/BindingFailures/Sentinel`: clear guarded outcomes |
| Task 9: complete integration | `ReportWorkflow.Run`, `ReportsConsole`, export | `A8FullNativeReportWorkflow`: real JSON -> native -> both reports -> JSON; `A8NativeFailurePreservesState`: failure leaves catalog, export and rental state unchanged; `A8NativeEmptyWorkflow`: empty/invalid data avoids adapter |
| Report/domain validation | `ReportRequest.Create`, `ReportInput.Validate` | `A8ReportRequestRules`, `A8InvalidCatalogRejected`: invalid ranges, fields and duplicates rejected before processing |
| Controlled file failures | `CatalogBoundary.Load`, workflow `Bind` | `A8MissingReportFile`, `A8MalformedReportFile`: FileMissing/MalformedJson, no continuation/output changes, cleanup succeeds |
| Earlier pure pipeline | `RentalPipeline`, `PricingPolicies` | Original 18 Assignment 5 assertions |
| Safe transitions/factory | `ValidatedRental`, `RentalWorkflow`, `RentalTariff` | Original 16 Assignment 6 assertions |
| Result/Option and explicit errors | `Result`, `Option`, `ValidatedRentalOutcomes`, `RentalTariffOutcomes`, `RentalOutcomes`, `CatalogBoundary` | Original 21 Assignment 7 assertions |

Latest verified outcome: **87/87 passed; 0 failed** on 2026-10-29. The suite contains
55 preserved tests plus 32 new tests (eight per member), including more than eight
pure/domain cases, two complete workflows and controlled missing/malformed-file
cases. The real C call is required for the full successful workflow. Expected console
output and the full latest test log are in `docs/EXPECTED_OUTPUT.txt` and
`docs/TEST_RESULTS.txt`. Build/run/test instructions were checked from a clean
extraction of the full ZIP; each sequential team change package was also checked.
