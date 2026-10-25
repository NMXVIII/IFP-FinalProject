using BoardGameRental.Core;
namespace BoardGameRental.Tests;
internal static partial class Program
{
    private static void A7CompleteBookingChain()
    {
        var result = RentalOutcomes.Book(Sample(), 2, 3, 1, 3000m, RentalTariff.Student);
        Check(result is Result<string>.Success ok && ok.Value ==
            "Confirmed: Uno; days: 3; quantity: 1; tariff: student; total: 1200.00 KZT.", "Incorrect integrated chain.");
    }
    private static void A7BookingDomainFailures()
    {
        A7ErrorIs(RentalOutcomes.Book(Sample(), 999, 3, 1, 3000m, RentalTariff.Student), "GameNotFound");
        A7ErrorIs(RentalOutcomes.Book(Sample(), 2, 3, 0, 3000m, RentalTariff.Student), "InvalidQuantity");
        A7ErrorIs(RentalOutcomes.Book(Sample(), 2, 3, 4, 3000m, RentalTariff.Student), "InsufficientStock");
        A7ErrorIs(RentalOutcomes.Book(Sample(), 2, 3, 1, 100m, RentalTariff.Student), "ValidationRejected");
    }
    private static void A7StatesAndRepeatedConfirmation()
    {
        var draft = new RentalState.Draft(A7ValidDraft());
        A7ErrorIs(RentalOutcomes.Confirm(draft), "InvalidState");
        Check(RentalWorkflow.Describe(draft) == "Draft", "Failed transition changed Draft.");
        RentalOutcomes.Prepare(draft).Match(ready =>
        {
            Check(ready is RentalState.Ready, "Preparation did not create Ready.");
            A7ErrorIs(RentalOutcomes.Prepare(ready), "InvalidState");
            return RentalOutcomes.Confirm(ready).Match(confirmed =>
            {
                Check(confirmed is RentalState.Confirmed, "Confirmation did not create Confirmed.");
                Check(RentalOutcomes.Confirm(confirmed) is Result<RentalState>.Success again && ReferenceEquals(confirmed, again.Value), "Repeated confirmation not idempotent.");
                A7ErrorIs(RentalOutcomes.Prepare(confirmed), "InvalidState");
                return 0;
            }, e => throw new InvalidOperationException(e.Message));
        }, e => throw new InvalidOperationException(e.Message));
    }
    private static void A7MissingLookupSkipsPreparation()
    {
        int calls = 0;
        var result = CatalogLookup.Find(Sample(), 999)
            .ToResult(new AppError("GameNotFound", "Game 999 was not found."))
            .Map<RentalState>(game => { calls++; return new RentalState.Draft(A7ValidDraft() with { Game = game }); })
            .Bind(state => { calls++; return RentalOutcomes.Prepare(state); })
            .Bind(state => { calls++; return RentalOutcomes.Confirm(state); });
        A7ErrorIs(result, "GameNotFound");
        Check(calls == 0, "Missing lookup must skip draft construction, preparation and confirmation.");
    }
    private static void A7BookingPreservesInput()
    {
        var catalog = Sample();
        var before = catalog.ToArray();
        var draft = new RentalState.Draft(A7ValidDraft() with { Budget = 1m });
        A7ErrorIs(RentalOutcomes.Prepare(draft), "ValidationRejected");
        Check(draft.Request.Budget == 1m && RentalWorkflow.Describe(draft) == "Draft", "Failed operation mutated state.");
        RentalOutcomes.Book(catalog, 2, 3, 1, 3000m, RentalTariff.Student);
        Same(before, catalog);
    }
    private static void A7FreeBooking()
    {
        var result = RentalOutcomes.Book(Sample(), 4, 3, 1, 0m, RentalTariff.Student);
        Check(result is Result<string>.Success ok && ok.Value.Contains("Chess") && ok.Value.Contains("total: 0.00 KZT"), "Valid zero-price booking rejected.");
    }
}
