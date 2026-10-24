# BoardGameRental — Assignment 6

The existing Assignment 5 recommendation pipeline remains available. Assignment 6
adds preparation and confirmation of one game's rental, with quantity and a
validated tariff. This is an in-memory confirmation of a catalog snapshot, not a
persistent stock reservation or payment system. No operation writes catalog data.

## Build and run

Prerequisite: .NET 8 SDK (`dotnet --version` must start with `8.`). SDK used: 8.0.425 on macOS (Apple Silicon).
No external NuGet packages. Windows, Linux or macOS with the SDK installed.
Working directory for every command: the extracted project folder, the one that
contains `BoardGameRental.sln`.

```sh
dotnet build BoardGameRental.sln
dotnet run --project src/BoardGameRental.App -- --demo
dotnet run --project tests/BoardGameRental.Tests
```

The tests are an assertions-based console runner, so use `dotnet run`, not
`dotnet test`. The process exits 1 on any failure and 0 on success.

Assignment 5 recommendations (unchanged calculation):

```sh
dotnet run --project src/BoardGameRental.App
dotnet run --project src/BoardGameRental.App -- sample-data/games.json 3 3000 both
```

Standard returns IDs 4, 2, 1 at 0, 1500, 3000 KZT. Student returns IDs 4, 2, 1, 6
at 0, 1200, 2400, 3000 KZT. Each recommendation is independently within budget.
`run.bat` and `test.bat` are optional Windows launchers for these existing commands.

Assignment 6 custom input: file, game ID, days, quantity, budget, tariff.

```sh
dotnet run --project src/BoardGameRental.App -- --book sample-data/games.json 2 3 1 3000 student
dotnet run --project src/BoardGameRental.App -- --book sample-data/games.json 2 3 4 10000 student
dotnet run --project src/BoardGameRental.App -- --book sample-data/games.json 2 3 1 100 student
```

The first confirms Uno at 1200.00 KZT and exits 0. The second rejects insufficient
stock, the third rejects insufficient budget; both exit 1. Unknown IDs and tariffs
are rejected. The `--demo` scenario also attempts confirmation before preparation
and repeats confirmation. Expected rejections inside the demo are demonstrations,
so a completed demo exits 0. Full expected output is in `docs/EXPECTED_OUTPUT.txt`.

## Hidden temporal dependency: unsafe before example

Documentation only; this code is not part of the active application:

```csharp
class UnsafeRental
{
    private decimal total;
    public decimal Confirm() => total;
    public void Calculate(decimal dailyPrice, int days)
    {
        total = dailyPrice * days;
    }
}

var rental = new UnsafeRental();
decimal wrong = rental.Confirm(); // 0: Calculate was forgotten.
rental.Calculate(500m, 3);
decimal correct = rental.Confirm(); // 1500, but only after the hidden step.
```

Prerequisite: the amount must be validated and calculated before confirmation.
Required hidden order: Calculate, then Confirm. Reversing or omitting Calculate
silently confirms a zero amount. Invalid days or prices also remain unchecked.

Safe call site used by the application:

```csharp
var draft = new RentalState.Draft(new RentalDraft(game, 3, 1, 3000m, RentalTariff.Student));
WorkflowResult prepared = RentalWorkflow.Prepare(draft);
if (prepared.IsSuccess)
{
    WorkflowResult confirmed = RentalWorkflow.Confirm(prepared.State);
}
```

Calling `Confirm(draft)` is allowed as a request but explicitly rejected with
"Prepare the rental before confirmation" and the same original Draft. No hidden
mutable flag or cached amount controls correctness.

## Factory, rules and access restrictions

`ValidatedRental.TryCreate` is used by `RentalWorkflow.Prepare`, which is used by
both `--book` and `--demo`. It returns a validated value or explicit error messages;
on failure the output value is null. All checks occur before construction.

- The application selects an existing ID from the validated catalog. Missing ID
  produces a null game and is rejected. The factory additionally revalidates the
  supplied game's fields; it does not authenticate membership in an external store.
- ID positive; name/category nonblank; price 0..1,000,000 KZT with at most two
  decimal places; stock nonnegative. Catalog loading also rejects duplicate IDs.
- Days 1..30 inclusive.
- Quantity positive and no greater than available copies.
- Budget 0..1,000,000,000 inclusive, with at most two decimal places.
- A supported non-null tariff is mandatory; only standard/student are booking
  tariffs. `both` is only a recommendation-display option from Assignment 5.
- Total price must be no greater than budget. Exact stock and budget limits pass.
- Student tariff rounds one copy's rental to two decimal places, away from zero
  at a midpoint, then multiplies by quantity. Standard is daily price * days * quantity.
- Zero-price games are valid; zero stock cannot satisfy a positive quantity.

`ValidatedRental` is sealed, has a private constructor and getter-only properties.
It is a class, not a record, so `with` cannot manufacture a modified validated value.
The following calls intentionally cannot compile and appear only here:

```csharp
// new ValidatedRental(...);       // constructor is private
// rental.Quantity = 0;            // no setter
// rental with { Quantity = 0 };   // not a record
// new RentalState.Ready(...);     // internal outside Core
```

`RentalTariff` also has a private constructor and fixed, read-only supported
instances. Raw `RentalDraft` values may be copied with invalid fields for validation;
they are not accepted as a confirmation-ready value.

## States and pattern matching

`RentalState` has a private base constructor and only three nested sealed cases.
External code cannot derive a fourth case. Ready and Confirmed constructors are
internal to Core, and both carry a factory-created `ValidatedRental`.

| Case | Payload | Meaning |
|---|---|---|
| Draft | RentalDraft | Raw request; prerequisites may be invalid |
| Ready | ValidatedRental | Rules checked, total calculated; can confirm |
| Confirmed | ValidatedRental | Successful immutable confirmation |

`Prepare`, `Confirm`, `Describe` and console output use explicit switch branches.
Unknown cases are rejected by an exception, never silently treated as success.
No payload casts or null-forgiving operators are used.

| Operation | Draft | Ready | Confirmed |
|---|---|---|---|
| Prepare | Validate; new Ready or rejection | Reject; preserve state | Reject; preserve state |
| Confirm | Reject; preserve state | Return new Confirmed | Return existing state successfully |

Repeated confirmation is idempotent: passing a Confirmed state back returns the
same object, without recalculation or a second update. Reusing an older Ready
snapshot creates another equivalent immutable confirmation; there is no database
reservation, charging or inventory mutation in this module. Concurrent inventory
and persistence would require an additional boundary transaction in a future stage.

A `WorkflowResult` carries the resulting/original state and optional error. Its
constructor is internal, properties have no setters, and `IsSuccess` is derived
from the absence of an error rather than independently editable. Failed transitions
return the exact original state. Source collections and stored JSON are unchanged.
File I/O and console output remain outside the pure factory/workflow calculations.

## Requirements and tests

| Requirement | Implementation | Assertions |
|---|---|---|
| 1: hidden dependency | Unsafe example above; BookingConsole safe call site | MissingPrerequisite |
| 2: validated factory | ValidatedRental.TryCreate; RentalTariff.TryCreate | FactorySuccess, FactoryRules, FactoryBoundaries, FactoryPreservesInput |
| 3: exclusive states | RentalWorkflow.cs / RentalState | WorkflowCases |
| 4: used matching | Prepare, Confirm, Describe; BookingConsole.Print | WorkflowCases, CatalogBooking |
| 5: safe operations | RentalWorkflow; immutable validated data | ConfirmationSuccess, MissingPrerequisite, RepeatedConfirmation, FailedPreparationPreservesState |
| Boundary values | Exact stock/days/budget; free games | FactoryBoundaries, StudentBooking |
| Pricing | RentalTariff, existing PricingPolicies | TariffNames, QuantityPricing, TariffRounding |
| Preservation | No domain or file updates | FactoryPreservesInput, FailedPreparationPreservesState, FileAndCatalogPreserved |
| Integration | CatalogFile + factory + workflow | CatalogBooking, UnknownCatalogGame |

34 cumulative tests: 18 retained from Assignment 5, 16 new for Assignment 6.
All test names and the latest actual result are in `docs/TEST_RESULTS.txt`.
Each test throws on mismatch; printing a scenario is not used as an assertion.
FactoryRules individually checks missing game, invalid game fields, both day limits,
nonpositive quantity, insufficient stock, invalid budget, missing tariff and price
above budget. WorkflowCases creates every declared state and verifies its branch.

### New tests and expected results (Assignment 6)

| Test | Owner | Expected result |
|---|---|---|
| FactorySuccess | Issen Daiana | Valid draft gives Uno, 3 days, quantity 1, total 1500, tariff "standard", no errors |
| FactoryRules | Issen Daiana | 18 invalid drafts (bad game fields, days 0 and 31, quantity 0, -1 and 4, bad budgets, no tariff, total above budget) are each rejected with the matching reason |
| FactoryBoundaries | Issen Daiana | 30 days, quantity 3 (all stock) and budget exactly 45000 accepted; free game at budget 0 and maximum budget accepted |
| FactoryPreservesInput | Issen Daiana | A failed creation leaves the draft unchanged; no public constructor; no writable properties |
| StudentBooking | Issen Daiana | Student tariff fits budget 2400; Standard tariff does not |
| WorkflowCases | Kurmashev Damir | Draft, Ready and Confirmed are each reachable and described correctly |
| ConfirmationSuccess | Kurmashev Damir | Confirmed total is 1500; the original draft is unchanged |
| MissingPrerequisite | Kurmashev Damir | Confirm on a Draft is rejected with "Prepare..." and returns the same state object |
| RepeatedConfirmation | Kurmashev Damir | Confirming twice returns the same object; Prepare on Ready or Confirmed is rejected |
| FailedPreparationPreservesState | Kurmashev Damir | Budget 1 is rejected with "exceeds budget"; the same state is returned |
| TariffNames | Kabdrakhmanova Zarina | " STANDARD " and "Student" are found; null, "", "both" and "vip" are rejected |
| QuantityPricing | Kabdrakhmanova Zarina | Two copies: Standard 3000, Student 2400 |
| TariffRounding | Kabdrakhmanova Zarina | Student, price 10.03, 1 day, quantity 3 gives 24.06 |
| CatalogBooking | Nurlanbek Marghulan | Uno from games.json is confirmed at 1200 with the student tariff |
| UnknownCatalogGame | Nurlanbek Marghulan | Game Id 999 is rejected and the draft state is unchanged |
| FileAndCatalogPreserved | Nurlanbek Marghulan | A capacity failure leaves the loaded catalog and the JSON file unchanged |

Latest verification: 2026-10-18, .NET SDK 8.0.425 on macOS (Apple Silicon), from a clean
extraction of the submitted ZIP. `dotnet build BoardGameRental.sln`: 0 warnings, 0 errors.
`dotnet run --project tests/BoardGameRental.Tests`: 34/34 passed. Full output: `docs/TEST_RESULTS.txt`.
`--demo` and no-argument output: `docs/EXPECTED_OUTPUT.txt`.

## Team responsibilities for this stage

| Member | Group | Logic implemented | Tests written | Documentation written |
|---|---|---|---|---|
| Kabdrakhmanova Zarina | IT-2511 | `RentalTariff.cs`: sealed class with a private constructor and fixed `Standard` and `Student` instances; `TryCreate(name)` normalises case and spaces and rejects unknown names such as "both"; `Calculate(game, days, quantity)` rounds one copy's price, then multiplies by quantity | `TariffTests.cs`: TariffNames, QuantityPricing, TariffRounding | Tariff and rounding rules |
| Issen Daiana | IT-2511 | `ValidatedRental.cs`: `RentalDraft` record and the `ValidatedRental` factory (`TryCreate`) with a private constructor and getter-only properties; checks game fields, days, quantity against stock, budget, tariff and total against budget; returns a value or all reasons, never a partial value | `FactoryTests.cs`: FactorySuccess, FactoryRules, FactoryBoundaries, FactoryPreservesInput, StudentBooking | Factory rules and access restrictions |
| Kurmashev Damir | IT-2511 | `RentalWorkflow.cs`: closed `RentalState` (Draft, Ready, Confirmed), `WorkflowResult`, and `Prepare`, `Confirm` and `Describe` using switch expressions; rejects invalid sequences and keeps the original state | `WorkflowTests.cs`: WorkflowCases, ConfirmationSuccess, MissingPrerequisite, RepeatedConfirmation, FailedPreparationPreservesState | Unsafe/safe dependency example and transition table |
| Nurlanbek Marghulan | IT-2511 | `BookingConsole.cs` (`Run` for `--book`, `Demo` for `--demo`, `Print`) and `App/Program.cs` routing: parses arguments, loads the catalog, builds the draft, calls Prepare then Confirm, sets exit codes; keeps file and console work outside the pure workflow | `BookingIntegrationTests.cs`: CatalogBooking, UnknownCatalogGame, FileAndCatalogPreserved; test runner registration of all 34 tests | Commands, requirement mapping, expected-results table, verification section |

These are the assigned ownership areas; each member must review, understand and
adapt their code, run relevant tests, and explain their contribution personally.

Earlier stages are kept in docs/assignment5/ (README, expected output and test results).
