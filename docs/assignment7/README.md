# BoardGameRental — Assignments 5, 6 and 7

A board-game rental application. A customer or clerk chooses a catalog game, a number
of days, a quantity, a budget and a tariff. The program finds the game, validates the
request, prices it and safely confirms the rental, or explains exactly why it cannot.
The catalog is read from a JSON file. The confirmation works on an immutable snapshot:
the project does not reduce stock and does not take payment.

Assignment 7 adds explicit failure handling: `Result<T>`, `Option<T>`, `Map`, `Bind` and
a single boundary where file and JSON exceptions are converted into outcomes. The
Assignment 5 pipeline and the Assignment 6 safe state transitions are still active.

## Build, run and test

Prerequisite: .NET 8 SDK (`dotnet --version` must start with `8.`). SDK used: 8.0.425
on macOS (Apple Silicon). `global.json` requires an 8.0.x SDK; SDK 10 alone is not enough.
No external NuGet packages. Windows, Linux or macOS.

Working directory for every command: the extracted project folder, the one that
contains `BoardGameRental.sln`.

```sh
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo7
```

Expected result of the tests: `55/55 passed; 0 failed.` (exit code 0, 1 on any failure).
The tests use a console assertion runner, so run them with `dotnet run`, not `dotnet test`.
Latest verification: 2026-10-25, .NET SDK 8.0.425 on macOS (Apple Silicon), from a clean
extraction of the submitted archive: build with 0 warnings and 0 errors, 55/55 tests.
Full output: `docs/TEST_RESULTS.txt`.
On Windows, `run.bat`, `test.bat` and `demo7.bat` run the same commands.

## Commands

| Command | Meaning |
|---|---|
| `--demo7` | Assignment 7 demonstration: success, every failure and both file failures |
| `--book7 <file> <gameId> <days> <quantity> <budget> <standard\|student>` | One booking through the Result chain. Exit code 0 on success, 1 on a rejection |
| `--demo`, `--book ...` | Assignment 6 demonstration and booking (unchanged) |
| no arguments, or `<file> <days> <budget> <both\|standard\|student>` | Assignment 5 recommendations (unchanged) |
| `--help` | List of modes |

Numbers use a dot as the decimal separator. Input data: `sample-data/games.json`
(6 games), `sample-data/malformed.json`, `sample-data/invalid.json`,
`sample-data/empty.json` (`[]`) and `sample-data/missing-catalog.json`.

## Domain rules

- Catalog record: positive unique ID; non-empty name and category; daily price 0 to
  1,000,000 with at most two decimals; available copies at least 0.
- A price of 0 and a stock of 0 are legitimate values and mean different things:
  a free game is bookable, a game with no copies is not.
- Request: game must exist; days 1 to 30; quantity greater than 0 and not above the
  available copies; budget 0 to 1,000,000,000 with at most two decimals; tariff
  `standard` or `student`; the total price must not exceed the budget.
- Student tariff gives a 20% discount. The price of one copy is rounded to two decimals
  (away from zero at the midpoint) and then multiplied by the quantity.
- A duplicate ID or any invalid record rejects the whole catalog. No partial catalog exists.

## Outcome model

`Result<T>` has exactly two cases. `Success` carries a non-null value. `Failure` carries
an `AppError(Code, Message)`. The value exists only in `Success` and the error only in
`Failure`, so a failure can never be read as if it had a value.
`Option<T>` has `Some(value)` and `None`. `None` becomes a failure only where the workflow
requires the item (`ToResult`). Zero, `false` and an empty string are `Some`, not absence.

`RentalState` (Draft, Ready, Confirmed) says where a rental is in its life cycle.
`Result` says whether one operation succeeded or why it was rejected. A validation
failure is not a new rental state: a failed `Prepare` leaves the Draft exactly as it was.

| Case | Success | Failure (code: context) |
|---|---|---|
| Find a required game | the found game | `GameNotFound`: the missing ID |
| Quantity | positive quantity | `InvalidQuantity`: the rejected quantity |
| Stock | quantity within stock | `InsufficientStock`: game ID, requested and available |
| Other factory rules (days, budget, tariff) | `ValidatedRental` with the calculated price | `ValidationRejected`: the broken rule |
| Tariff name | normalized tariff | `UnknownTariff`: the given name, allowed values |
| Prepare, Confirm | Draft -> Ready -> Confirmed | `InvalidState`: current state and missing prerequisite |
| Load catalog | fully validated read-only catalog | `FileMissing`, `MalformedJson`, `InvalidCatalog`, `AccessDenied`, `FileReadFailed`, `InvalidPath`: operation and path |

Expected business failures are returned, never thrown. Only file and JSON exceptions are
caught, in one place (`CatalogBoundary.Load`). There is no fallback and no data is written.

The booking chain (`RentalOutcomes.Book`):

```text
Find -> ToResult -> Map(create Draft) -> Bind(Prepare) -> Bind(Confirm) -> Map(text)
```

`Map` takes `T -> U` and wraps the new value itself. `Bind` takes `T -> Result<U>` and
returns the next operation's own Result, so no `Result<Result<U>>` appears. On a failure
both keep the error and do not call the supplied function. The console chain is
`Load -> Bind(Parse tariff -> Bind(Book))`, handled once in `ExceptionConsole.Print`.

## Requirement map

| Requirement | Files and methods | Tests |
|---|---|---|
| Task 1: explicit outcomes, 3+ domain failures | `Result.cs`; `ValidatedRentalOutcomes.Create`; `RentalOutcomes` | A7ValidatedFactorySuccess, A7ValidatedFactoryFailures, A7BookingDomainFailures, A7RejectsNullPayload |
| Task 2: missing values | `Option.cs` (`CatalogLookup.Find`, `FindDailyPrice`, `ToResult`); `RentalTariffOutcomes.Parse` | A7LookupPresentAbsent, A7PrimitiveValuesPresent, A7MissingBecomesFailure, A7TariffLookupSuccess/Failure |
| Task 3: Map and Bind in a real chain | `Result.Map/Bind/Match`; `RentalOutcomes.Book`; `ExceptionConsole.Run` | A7MapSuccess, A7BindSuccess, A7CompleteBookingChain |
| Dependent functions skipped after failure | `Result.Map/Bind` | A7FailureSkipsFunctions, A7MissingLookupSkipsPreparation (call counter is 0) |
| Task 4: exception boundary | `CatalogBoundary.Load` | A7MissingFilePreservesState, A7MalformedFilePreservesState, A7InvalidCatalogAndEmptyFile |
| Task 5: integration, earlier stages kept | `ExceptionConsole.Run/Demo`; `Program.Main` routes; old commands | A7CompleteFileToBookingChain; all 34 earlier tests; `--demo7` |
| Safe transitions, zero price | `RentalOutcomes.Prepare/Confirm` | A7StatesAndRepeatedConfirmation, A7BookingPreservesInput, A7FreeBooking |

## Demonstrations (`--demo7` shows all of them)

```sh
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 1 3000 student   # success: Uno 1200.00 KZT
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 999 3 1 3000 student # GameNotFound (missing lookup)
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 0 3000 student   # InvalidQuantity
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 4 3000 student   # InsufficientStock
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 1 100 student    # ValidationRejected: budget
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 2 3 1 3000 vip       # UnknownTariff
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/games.json 4 3 1 0 student      # free game, zero budget: accepted
dotnet run --project src/BoardGameRental.App -- --book7 sample-data/malformed.json 2 3 1 3000 student # MalformedJson
```

`--demo7` also shows `InvalidState` and a missing file (in a temporary folder created and
removed by the demo, so it never depends on the laptop). Rejections inside the finished
demonstration are expected, so `--demo7` exits with 0. A single `--book7` exits with 1 on
any rejection. Wrong arguments print an explanation and exit with 1.
Expected output: `docs/EXPECTED_OUTPUT.txt`.

## Tests

55 cumulative tests: 34 from Assignments 5 and 6 plus 21 new. Every test compares actual
and expected values and fails on a mismatch. File tests use unique temporary folders that are
removed in `finally`; fixtures are in `sample-data/`.

| Member | Test | Expected result |
|---|---|---|
| Issen Daiana | A7MapSuccess | 7 reaches Map and the result is 21 |
| Issen Daiana | A7BindSuccess | Bind receives 7 and returns the next Result itself |
| Issen Daiana | A7FailureSkipsFunctions | After a failure the Map/Bind counter is 0 and the same error object is kept |
| Issen Daiana | A7RejectsNullPayload | A Success with a null payload cannot be created |
| Issen Daiana | A7ValidatedFactorySuccess | Uno, 3 days, 1 copy, student: 1200 KZT; input unchanged |
| Issen Daiana | A7ValidatedFactoryFailures | GameNotFound, InvalidQuantity, InsufficientStock, ValidationRejected |
| Kabdrakhmanova Zarina | A7LookupPresentAbsent | Uno is found; ID 999 gives None |
| Kabdrakhmanova Zarina | A7PrimitiveValuesPresent | Price 0, `false` and an empty string are present values |
| Kabdrakhmanova Zarina | A7MissingBecomesFailure | None becomes GameNotFound; Some becomes Success |
| Kabdrakhmanova Zarina | A7TariffLookupSuccess | " STANDARD " and "Student" are normalized; student price 1200 |
| Kabdrakhmanova Zarina | A7TariffLookupFailure | null, "", "both" and "vip" give UnknownTariff |
| Kurmashev Damir | A7CompleteBookingChain | Exact confirmation text for Uno, 1200.00 KZT |
| Kurmashev Damir | A7BookingDomainFailures | GameNotFound, InvalidQuantity, InsufficientStock, ValidationRejected |
| Kurmashev Damir | A7StatesAndRepeatedConfirmation | Draft cannot be confirmed; Ready/Confirmed cannot be prepared; repeated Confirm returns the same object |
| Kurmashev Damir | A7MissingLookupSkipsPreparation | After a missing game, Draft creation, Prepare and Confirm are not called |
| Kurmashev Damir | A7BookingPreservesInput | A failed operation leaves the Draft and the catalog unchanged |
| Kurmashev Damir | A7FreeBooking | Chess at price 0 is accepted with budget 0 |
| Nurlanbek Marghulan | A7MissingFilePreservesState | FileMissing contains the path, creates no file, skips preparation, keeps the Draft |
| Nurlanbek Marghulan | A7MalformedFilePreservesState | Bad syntax and a wrong value type give MalformedJson; file and state unchanged |
| Nurlanbek Marghulan | A7InvalidCatalogAndEmptyFile | A catalog with one bad record is rejected whole; `[]` is accepted |
| Nurlanbek Marghulan | A7CompleteFileToBookingChain | File -> tariff -> booking gives 1200.00; "vip" stops the chain; file unchanged |

## Team

| Member | Group | Assignment 7 logic | Tests | Documentation |
|---|---|---|---|---|
| Issen Daiana | IT-2511 | `Result.cs` (Success, Failure, Map, Bind, Match); `ValidatedRentalOutcomes.cs` reusing the Assignment 6 factory | `ResultFactoryTests.cs` | Outcome model and failure table |
| Kabdrakhmanova Zarina | IT-2511 | `Option.cs` (Some, None, `CatalogLookup`); `RentalTariffOutcomes.cs` | `OptionTariffTests.cs` | Missing-value rules |
| Kurmashev Damir | IT-2511 | `RentalOutcomes.cs`: find, prepare, confirm chain with Map and Bind | `ResultWorkflowTests.cs` | Transitions and the booking chain |
| Nurlanbek Marghulan | IT-2511 | `CatalogBoundary.cs` (file and JSON boundary); `ExceptionConsole.cs` and `Program.cs` routes; one final outcome handler | `BoundaryOutcomeTests.cs` | Commands, requirement map, expected results, verification |

Each member can explain and defend their own part.

## Earlier stages

Earlier stages are kept in `docs/assignment5/` and `docs/assignment6/` (README, expected
output and test results). Assignment 5 is the three-stage pure pipeline; Assignment 6 is the
`ValidatedRental` factory and the Draft, Ready, Confirmed states.
