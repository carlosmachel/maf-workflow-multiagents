using Console.App.Frauds;
using Console.App.Incomes;
using Console.App.Kycs;

namespace Console.App.Executors;

public sealed class DecisionResult
{
    public string? Outcome { get; set; }
    public string[]? Conditions { get; set; }
    public string? Summary { get; set; }
    public DecisionDetails? Details { get; set; }
}

public sealed class DecisionDetails
{
    public KycResult? Kyc { get; set; }
    public FraudResult? Fraud { get; set; }
    public IncomeResult? Income { get; set; }
}