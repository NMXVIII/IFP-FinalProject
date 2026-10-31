# BoardGameRental - Assignment 8 development snapshot

This snapshot includes the first 2 Assignment 8 modules. Assignments 5-7 remain runnable. The final native console/report integration is included in module 4.

## Build and test

Use Windows x64 and .NET SDK 8.0.425. Open PowerShell in the folder containing `BoardGameRental.sln`. No NuGet packages or C compiler are needed for this intermediate snapshot.

```powershell
dotnet build BoardGameRental.sln
dotnet run --project tests/BoardGameRental.Tests
dotnet run --project src/BoardGameRental.App -- --demo7
```

Latest verification on 2026-10-29: **71/71 passed; 0 failed**, build with 0 warnings/errors. The original 55 tests remain included. New query modules are exercised by assertions; the Assignment 8 application entry point is added with the native integration module.

## Team, group IT-2511

| Member | Module and tests | Documentation |
|---|---|---|
| Issen Daiana | Report records, extensions, affordable query and eight ReportQueryTests cases | Query stages, receiver contracts and lazy timing |
| Kabdrakhmanova Zarina | Configured prices, category query, input rules and eight ReportConfigurationTests cases | Currying, grouping and totals |
| Kurmashev Damir | ReportWorkflow, ReportExport and eight ReportWorkflowTests cases | Snapshots, file safety and resource ownership |
| Nurlanbek Marghulan | Native C, adapter, console integration and eight NativeReportTests cases | Native build/loading and full run procedure |

## Implemented queries

`AvailableForRental(this IEnumerable<BoardGame>)` defers filtering stock > 0 and returns a game sequence. It belongs on a collection because eligibility selects its members. `QuoteFor(this BoardGame, days, configuredPrice)` returns a RentalOffer with identity, category, days and a supplied one-copy calculation. It belongs on a game because that record supplies the offer fields. Neither operation changes shared state.

`RentalReports.AffordableRentals` uses both extensions: Where -> Select -> Where -> OrderBy(price) -> ThenBy(ID), followed by yield return. Inputs must be validated and the supplied price function pure. For the sample, 3 days and budget 3000, standard IDs are 4,2,1 with totals 0,1500,3000. Empty and no-match input gives an empty report; equal prices sort by ID.

The iterator does no processing until enumeration. Its ordering buffers input before yielding the first row. Test-local probes observe 0 records/prices before iteration and 6/5 afterward. A second enumeration recomputes. The source enumerator is disposed after processing or early stop; the query opens no resource.

## Configuration and category totals

`ReportPricing.CreatePrice` is curried: tariff -> days -> game -> price. Fixing Standard/Student and 3 days creates two functions used on the same catalog. Uno costs 1500 versus 1200 KZT; student adds Carcassonne at the 3000 budget. The closures retain only tariff and duration.

`CategoryReports.Inventory` consumes StockAssessment records, groups by ordinal case-sensitive category, projects Count/Sum totals, orders by daily value descending then category ascending. Expected categories: Strategy(3 titles,3 copies,4000 KZT,3 to buy), Family(1,2,2000,0), Card(1,3,1500,0), Classic(1,1,0,1). The native assessments are supplied by module 4; current grouping assertions use manually specified assessment fixtures.

`ReportRequest.Create` validates days 1..30, budget 0..1000000000 with at most 2 decimals, and target copies 0..Int32.MaxValue. ReportInput reuses the earlier full-catalog validation rules. Zero price and zero stock remain distinct; malformed/duplicate catalog rows are rejected.

## Earlier behaviour

No-argument application launch retains the pure rental pipeline. `--demo` retains safe Draft/Ready/Confirmed transitions; `--demo7` demonstrates Result/Option and protected file boundaries. Booking Uno with `--book7 sample-data/games.json 2 3 1 3000 student` yields 1200.00 KZT. Catalog inputs and safe rental states are not modified by report calculations. See docs/TEST_RESULTS.txt for every current test name and outcome.
