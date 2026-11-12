using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static T FinalValue<T>(Result<T> result) where T : notnull =>
        result.Match(value => value, error => throw new InvalidOperationException(error.Message));

    private static void FinalReturnLate()
    {
        var state = FinalValue(RentalOutcomes.BookState(Sample(), 2, 3, 1, 3000m, RentalTariff.Student));
        ReturnReceipt receipt = FinalValue(RentalReturn.Settle(state, 5));
        Check(receipt == new ReturnReceipt("Uno", 1, 5, 2, 800m, 2000m), "Unexpected late settlement.");
    }

    private static void FinalReturnEarlyOrOnTime()
    {
        var state = FinalValue(RentalOutcomes.BookState(Sample(), 2, 3, 2, 3000m, RentalTariff.Student));
        foreach (int actualDays in new[] { 2, 3 })
        {
            ReturnReceipt receipt = FinalValue(RentalReturn.Settle(state, actualDays));
            Check(receipt.OverdueDays == 0 && receipt.LateCharge == 0m && receipt.FinalCharge == 2400m,
                "Early or on-time return changed the original charge.");
        }
    }

    private static void FinalReturnInvalidState()
    {
        var draft = new RentalState.Draft(A7ValidDraft());
        var ready = FinalValue(RentalOutcomes.Prepare(draft));
        A7ErrorIs(RentalReturn.Settle(draft, 3), "InvalidState");
        A7ErrorIs(RentalReturn.Settle(ready, 3), "InvalidState");
    }

    private static void FinalReturnInvalidDays()
    {
        var state = FinalValue(RentalOutcomes.BookState(Sample(), 2, 3, 1, 3000m, RentalTariff.Student));
        A7ErrorIs(RentalReturn.Settle(state, 0), "InvalidReturnDays");
        A7ErrorIs(RentalReturn.Settle(state, 366), "InvalidReturnDays");
        Check(FinalValue(RentalReturn.Settle(state, 365)).OverdueDays == 362, "Upper bound rejected.");
    }
}
