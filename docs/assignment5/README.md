# BoardGameRental — Assignment 5 (Composing Functions Using Pipelining)

Console application for a small board-game rental shop. **Users:** shop staff who help a customer choose a game for a number of days within a budget.
**Operation implemented:** recommend available games for `days` and `budget`, priced by a chosen pricing policy.
This is the first module of the final project (checkout, returns, Result/Maybe etc. are future work and are not claimed here).

## Team and contributions

| Member | Group | Implemented logic | Tests | Documentation / other |
|---|---|---|---|---|
| Kurmashev Damir | IT-2511 | `RentalPipeline.cs` (three stages + `Run`), solution/project files, `Directory.Build.props` | NormalWorkflow, BudgetBoundary, AvailabilityStage, DelegateInvocation, PurityAndOwnership, StableOrdering, NoMatches (`PipelineTests.cs`) | Stage descriptions |
| Issen Daiana | IT-2511 | `Models.cs`, `InputValidation.cs` (catalog and request validation) | LegitimateZeros, MissingInput, InvalidFields, DuplicateIds, RequestValidation (`ValidationTests.cs`) | Input rules table |
| Kabdrakhmanova Zarina | IT-2511 | `PricingPolicies.cs` (Standard and Student policies) | TwoPolicies, Rounding (`PricingTests.cs`) | Policy description |
| Nurlanbek Marghulan | IT-2511 | `CatalogFile.cs` (JSON file loading), `App/Program.cs` (arguments, policy selection, output), test runner `tests/.../Program.cs` | EmptyDataset, InvalidFile, MalformedFile, MissingFile (`FileTests.cs`) | This README, `sample-data/` |


## Requirements and setup

- **.NET 8 SDK** (verify with `dotnet --version`; it should start with `8.`). `global.json` selects an installed 8.0.x SDK. Target framework: `net8.0`. Works on Windows, macOS (Intel and Apple Silicon) and Linux.
- No NuGet packages, database or internet access are needed. `NuGet.Config` clears external package sources because the project has no package dependencies.
- Extract the archive. Open a terminal in the folder that contains `BoardGameRental.sln` (this is the expected working directory for all commands).

```sh
dotnet build BoardGameRental.sln
dotnet run --project src/BoardGameRental.App
dotnet run --project tests/BoardGameRental.Tests
```

The third command is the **test command**. The tests use an assertions-based console runner (allowed by the brief): each check throws on a mismatch, `Main` prints PASS/FAIL per test and a total, and the exit code is 1 if any test fails, 0 otherwise. `dotnet test` is not used.

Windows shortcuts: `run.bat` runs the default demo and `test.bat` runs the tests (each changes to its own folder first). On macOS/Linux use the commands above.

Sample JSON files are copied next to each executable, so the commands work from the solution folder.

## Demonstrations

With no arguments the app runs the documented demo: `games.json`, 3 days, budget 3000 KZT, both policies. Custom mode needs all four arguments: `<file> <days> <budget> <both|standard|student>`.

```sh
# Success: both policies on the same input
dotnet run --project src/BoardGameRental.App -- sample-data/games.json 3 3000 both
# One policy
dotnet run --project src/BoardGameRental.App -- sample-data/games.json 3 3000 student
# Valid empty catalog (success, no matches)
dotnet run --project src/BoardGameRental.App -- sample-data/empty.json 3 3000 both
# Failures (exit code 1, message on stderr, no result)
dotnet run --project src/BoardGameRental.App -- sample-data/invalid.json 3 3000 both
dotnet run --project src/BoardGameRental.App -- sample-data/missing-catalog.json 3 3000 both
dotnet run --project src/BoardGameRental.App -- sample-data/malformed.json 3 3000 both
dotnet run --project src/BoardGameRental.App -- sample-data/does-not-exist.json 3 3000 both
dotnet run --project src/BoardGameRental.App -- sample-data/games.json 0 3000 both
```

## Models and rules

`RawGame` (nullable raw fields, so *missing* differs from `0`) → validated `BoardGame` → `RentalQuote`. `RentalRequest` holds days and budget. All are immutable records.

| Field | Rule |
|---|---|
| Id | Required positive integer, unique in the catalog |
| Name, Category | Required, not blank; trimmed |
| DailyPrice | Required, 0–1,000,000 KZT inclusive, at most 2 decimals (0 = free game) |
| AvailableCopies | Required integer ≥ 0 (0 = valid but unavailable) |
| Days | Required integer 1–30 |
| Budget | Required, 0–1,000,000,000 KZT, at most 2 decimals, decimal dot |

- A missing field or a JSON `null` catalog is rejected; `[]` is a valid empty catalog.
- If any record is invalid the whole catalog is rejected with messages (no partial success).
- Available means `AvailableCopies > 0`. A quote is recommended if `TotalPrice <= Budget` (boundary included). Order: price ascending, then Id ascending.
- **Standard** policy: `DailyPrice * Days`. **Student** policy: `Round(DailyPrice * Days * 0.80, 2, AwayFromZero)`.

## Dataset and expected results (documented before running)

`sample-data/games.json`, 3 days, budget 3000 KZT:

| Id | Game | Daily | Copies | Standard | Student | Result |
|---|---|---:|---:|---:|---:|---|
| 1 | Monopoly | 1000 | 2 | 3000 | 2400 | both; exact boundary under Standard |
| 2 | Uno | 500 | 3 | 1500 | 1200 | both |
| 3 | Catan | 1500 | 1 | 4500 | 3600 | neither (over budget) |
| 4 | Chess | 0 | 1 | 0 | 0 | both (zero price is valid) |
| 5 | Risk | 1200 | 0 | – | – | neither (unavailable, removed in stage 1) |
| 6 | Carcassonne | 1250 | 2 | 3750 | 3000 | **Student only** (policy changes result; exact boundary) |

Expected Standard: **Chess 0.00, Uno 1500.00, Monopoly 3000.00**.
Expected Student: **Chess 0.00, Uno 1200.00, Monopoly 2400.00, Carcassonne 3000.00**.

Other files: `empty.json` (`[]`), `invalid.json` (negative price, empty name, missing price, negative stock), `malformed.json` (broken JSON), `missing-catalog.json` (`null`).

## Pipeline stages (`RentalPipeline.cs`)

`Run(games, request, pricePolicy)` calls the three stages in order, passing each result to the next.

| Stage | Input → output | Rule and example |
|---|---|---|
| 1 `SelectAvailableGames` | `IReadOnlyList<BoardGame>` → `IReadOnlyList<BoardGame>` | Keep `AvailableCopies > 0`. Ids 1–6 → 1,2,3,4,6 (Risk removed). |
| 2 `CalculateQuotes` | games + `days` + `Func<BoardGame,int,decimal>` → `IReadOnlyList<RentalQuote>` | Calls the policy for every game. Monopoly, 3 days, Standard → 3000. |
| 3 `BuildRecommendations` | quotes + `budget` → `IReadOnlyList<RentalQuote>` | Keep `TotalPrice <= budget`, sort by price then Id. 3000 KZT, Standard → Ids 4,2,1. |

**Higher-order function:** `CalculateQuotes` (and `Run`, which passes the delegate on) takes a `Func<BoardGame,int,decimal>` and invokes `pricePolicy(game, days)`. The policy is chosen in `App/Program.cs` (`PricingPolicies.Standard` or `PricingPolicies.Student`) and passed in as an argument; the pipeline contains no policy name, global setting or `if` on the policy.

**Purity:** stages never print, read files, use a clock or random values, or modify their inputs. Each stage returns a new read-only wrapper over a fresh array, so later changes to the caller's list cannot change a computed result. Files are read only in `CatalogFile.cs`; arguments and printing only in `App/Program.cs`.

## Requirement map

| Requirement | Where |
|---|---|
| Task 1 workflow, ≥5 records, expected results | this README, `sample-data/games.json` |
| Task 2 typed models, validation, missing vs empty | `Models.cs`, `InputValidation.cs`, `CatalogFile.cs` |
| Task 3 three connected stages | `RentalPipeline.Run`, `SelectAvailableGames`, `CalculateQuotes`, `BuildRecommendations` |
| Task 4 higher-order function, two policies | `CalculateQuotes`; `PricingPolicies.Standard`, `Student`; policy chosen in `App/Program.cs` |
| Task 5 pure core, separate I/O | `RentalPipeline.cs`, `PricingPolicies.cs` vs `CatalogFile.cs`, `App/Program.cs` |
| Automated tests | `tests/BoardGameRental.Tests/` |

## Automated tests (18)

| # | Test | Expected result |
|---:|---|---|
| 1 | NormalWorkflow | Standard: Ids 4,2,1; totals 0,1500,3000 |
| 2 | TwoPolicies | Student: Ids 4,2,1,6; totals 0,1200,2400,3000; Standard has 3 results, Student 4 |
| 3 | EmptyDataset | `[]` loads; result is empty |
| 4 | BudgetBoundary | 3000 included, 3000.03 excluded |
| 5 | LegitimateZeros | free game kept, zero-stock game excluded |
| 6 | MissingInput | null catalog, null record, JSON `null` rejected |
| 7 | InvalidFields | each invalid field rejected; no partial data |
| 8 | DuplicateIds | rejected with message |
| 9 | RequestValidation | days 1/30 and budgets 0/max accepted; bad values rejected |
| 10 | AvailabilityStage | Ids 1,2,3,4,6 |
| 11 | DelegateInvocation | delegate called 5 times; its results are used |
| 12 | PurityAndOwnership | input unchanged; same result twice; independent collections |
| 13 | StableOrdering | equal prices ordered by Id; input order unchanged |
| 14 | Rounding | 10.03 × 0.80 → 8.02 |
| 15 | NoMatches | valid data, empty result |
| 16 | InvalidFile | `invalid.json` reports ≥3 errors, no data |
| 17 | MalformedFile | message contains "Invalid JSON" |
| 18 | MissingFile | failure reported, no data |

### Latest test outcome

The full output of the latest run (date, SDK version, 18 PASS lines) is saved in `docs/TEST_RESULTS.txt`; the demo output is in `docs/EXPECTED_OUTPUT.txt`.

Result: 18/18 passed (SDK 8.0.425, date 2026-10-13).
