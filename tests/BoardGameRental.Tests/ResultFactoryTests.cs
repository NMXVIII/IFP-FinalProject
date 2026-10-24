using BoardGameRental.Core;
namespace BoardGameRental.Tests;
internal static partial class Program
{
    private static void A7ErrorIs<T>(Result<T> result, string code) where T : notnull =>
        Check(result is Result<T>.Failure bad && bad.Error.Code == code && bad.Error.Message.Length > 0,
            $"Expected useful {code} failure.");
    private static RentalDraft A7ValidDraft() =>
        new(Sample().Single(g => g.Id == 2), 3, 1, 3000m, RentalTariff.Student);
    private static void A7MapSuccess()
    {
        int seen = -1;
        var result = new Result<int>.Success(7).Map(n => { seen = n; return n * 3; });
        Check(seen == 7 && result is Result<int>.Success ok && ok.Value == 21, "Map must transform the successful payload.");
    }
    private static void A7BindSuccess()
    {
        var expected = new Result<string>.Success("continued");
        int seen = -1;
        var result = new Result<int>.Success(7).Bind(n => { seen = n; return expected; });
        Check(seen == 7 && ReferenceEquals(result, expected), "Bind must preserve the continuation Result.");
        var failure = new Result<string>.Failure(new AppError("Rejected", "Next operation rejected"));
        Check(ReferenceEquals(new Result<int>.Success(7).Bind(_ => failure), failure), "Bind lost continuation failure.");
    }
    private static void A7FailureSkipsFunctions()
    {
        var error = new AppError("Stop", "Do not continue");
        int calls = 0;
        var result = new Result<int>.Failure(error)
            .Map(n => { calls++; return n + 1; })
            .Bind(n => { calls++; return new Result<string>.Success(n.ToString()); });
        Check(calls == 0 && result is Result<string>.Failure bad && ReferenceEquals(bad.Error, error),
            "Map/Bind must skip functions and preserve the error after failure.");
    }
    private static void A7RejectsNullPayload()
    {
        bool rejected = false;
        try { _ = Activator.CreateInstance(typeof(Result<string>.Success), new object?[] { null }); }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is ArgumentNullException) { rejected = true; }
        Check(rejected, "Success accepted a missing required payload.");
    }
    private static void A7ValidatedFactorySuccess()
    {
        var draft = A7ValidDraft();
        var result = ValidatedRentalOutcomes.Create(draft);
        Check(result is Result<ValidatedRental>.Success ok && ok.Value.TotalPrice == 1200m &&
            ok.Value.Game.Name == "Uno" && ok.Value.Quantity == 1, "Incorrect validated outcome.");
        Check(draft == A7ValidDraft(), "Factory changed input.");
    }
    private static void A7ValidatedFactoryFailures()
    {
        var draft = A7ValidDraft();
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Game = null }), "GameNotFound");
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Quantity = 0 }), "InvalidQuantity");
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Quantity = -1 }), "InvalidQuantity");
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Quantity = 4 }), "InsufficientStock");
        var budget = ValidatedRentalOutcomes.Create(draft with { Budget = 100m });
        A7ErrorIs(budget, "ValidationRejected");
        Check(budget is Result<ValidatedRental>.Failure bad && bad.Error.Message.Contains("exceeds budget"), "Budget rule missing.");
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Days = 0 }), "ValidationRejected");
        A7ErrorIs(ValidatedRentalOutcomes.Create(draft with { Tariff = null }), "ValidationRejected");
        Check(draft.Quantity == 1 && draft.Budget == 3000m, "Rejected validation changed draft.");
    }
}
