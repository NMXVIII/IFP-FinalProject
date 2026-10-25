using BoardGameRental.Core;

namespace BoardGameRental.Tests;

internal static partial class Program
{
    private static int Main()
    {
        (string Name, Action Test)[] tests =
        {
            ("Normal composed workflow", NormalWorkflow),
            ("Both policies change the documented result", TwoPolicies),
            ("Valid empty dataset", EmptyDataset),
            ("Exact budget included, above budget excluded", BudgetBoundary),
            ("Zero price and zero stock have different meanings", LegitimateZeros),
            ("Missing catalog and null record rejected", MissingInput),
            ("Every required catalog field is validated", InvalidFields),
            ("Duplicate IDs rejected", DuplicateIds),
            ("Request boundaries and malformed input", RequestValidation),
            ("Availability stage independently checked", AvailabilityStage),
            ("Supplied delegate is invoked on selected records", DelegateInvocation),
            ("Input unchanged, repeatable result, isolated collections", PurityAndOwnership),
            ("Equal prices use ID as a tie-breaker", StableOrdering),
            ("Student price rounds away from zero", Rounding),
            ("Valid catalog with no matches", NoMatches),
            ("Invalid sample produces no partial catalog", InvalidFile),
            ("Malformed JSON rejected", MalformedFile),
            ("Missing file reported safely", MissingFile),
            ("FactorySuccess", FactorySuccess),
            ("FactoryRules", FactoryRules),
            ("FactoryBoundaries", FactoryBoundaries),
            ("FactoryPreservesInput", FactoryPreservesInput),
            ("WorkflowCases", WorkflowCases),
            ("ConfirmationSuccess", ConfirmationSuccess),
            ("MissingPrerequisite", MissingPrerequisite),
            ("RepeatedConfirmation", RepeatedConfirmation),
            ("FailedPreparationPreservesState", FailedPreparationPreservesState),
            ("TariffNames", TariffNames),
            ("QuantityPricing", QuantityPricing),
            ("TariffRounding", TariffRounding),
            ("StudentBooking", StudentBooking),
            ("CatalogBooking", CatalogBooking),
            ("UnknownCatalogGame", UnknownCatalogGame),
            ("FileAndCatalogPreserved", FileAndCatalogPreserved),
            ("A7MapSuccess", A7MapSuccess),
            ("A7BindSuccess", A7BindSuccess),
            ("A7FailureSkipsFunctions", A7FailureSkipsFunctions),
            ("A7RejectsNullPayload", A7RejectsNullPayload),
            ("A7ValidatedFactorySuccess", A7ValidatedFactorySuccess),
            ("A7ValidatedFactoryFailures", A7ValidatedFactoryFailures),
            ("A7LookupPresentAbsent", A7LookupPresentAbsent),
            ("A7PrimitiveValuesPresent", A7PrimitiveValuesPresent),
            ("A7MissingBecomesFailure", A7MissingBecomesFailure),
            ("A7TariffLookupSuccess", A7TariffLookupSuccess),
            ("A7TariffLookupFailure", A7TariffLookupFailure),
            ("A7CompleteBookingChain", A7CompleteBookingChain),
            ("A7BookingDomainFailures", A7BookingDomainFailures),
            ("A7StatesAndRepeatedConfirmation", A7StatesAndRepeatedConfirmation),
            ("A7MissingLookupSkipsPreparation", A7MissingLookupSkipsPreparation),
            ("A7BookingPreservesInput", A7BookingPreservesInput),
            ("A7FreeBooking", A7FreeBooking)
        };

        int failures = 0;
        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS: {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine($"FAIL: {name}: {exception.Message}");
            }
        }
        Console.WriteLine($"\n{tests.Length - failures}/{tests.Length} passed; {failures} failed.");
        return failures == 0 ? 0 : 1;
    }

    private static IReadOnlyList<BoardGame> Sample()
    {
        bool success = CatalogFile.TryLoad(SamplePath("games.json"), out var games, out var errors);
        Check(success, string.Join("; ", errors));
        return games;
    }

    private static string SamplePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "sample-data", name);

    private static RawGame ValidRaw() => new(1, "Uno", "Card", 500m, 2);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException($"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
    }
}
