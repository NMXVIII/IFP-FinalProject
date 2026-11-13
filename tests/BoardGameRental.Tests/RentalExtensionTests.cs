using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static RentalState ConfirmedUno() =>
        FinalValue(RentalOutcomes.BookState(Sample(), 2, 3, 1, 3000m, RentalTariff.Student));

    private static void FinalExtensionSuccess()
    {
        var original = (RentalState.Confirmed)ConfirmedUno();
        ExtensionReceipt receipt = FinalValue(RentalExtension.Extend(original, 2, 2000m));
        Check(receipt.Rental.Rental.Days == 5 && receipt.Rental.Rental.TotalPrice == 2000m &&
            receipt.AdditionalCharge == 800m, "Incorrect extended rental.");
        Check(original.Rental.Days == 3 && original.Rental.TotalPrice == 1200m, "Original rental changed.");
    }

    private static void FinalExtensionInvalidState()
    {
        var draft = new RentalState.Draft(A7ValidDraft());
        A7ErrorIs(RentalExtension.Extend(draft, 2, 3000m), "InvalidState");
        A7ErrorIs(RentalExtension.Extend(FinalValue(RentalOutcomes.Prepare(draft)), 2, 3000m), "InvalidState");
    }

    private static void FinalExtensionDuration()
    {
        var original = ConfirmedUno();
        A7ErrorIs(RentalExtension.Extend(original, 0, 3000m), "InvalidExtensionDays");
        A7ErrorIs(RentalExtension.Extend(original, int.MaxValue, 3000m), "InvalidExtensionDays");
        Check(FinalValue(RentalExtension.Extend(original, 27, 12000m)).Rental.Rental.Days == 30,
            "Maximum duration rejected.");
    }

    private static void FinalExtensionBudget()
    {
        var original = (RentalState.Confirmed)ConfirmedUno();
        A7ErrorIs(RentalExtension.Extend(original, 2, 1999.99m), "ValidationRejected");
        Check(original.Rental.TotalPrice == 1200m && original.Rental.Days == 3, "Failed extension changed state.");
    }
}
