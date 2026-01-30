using System.Text.Json;
using Console.App.Frauds;
using Console.App.Incomes;
using Console.App.Kycs;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Console.App.Executors;

/// <summary>
/// Executor that aggregates the results from the concurrent agents.
/// </summary>
internal sealed class ConcurrentAggregationExecutor() :
    Executor<List<ChatMessage>>("ConcurrentAggregationExecutor")
{
    private readonly List<ChatMessage> _messages = [];
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Handles incoming messages from the agents and aggregates their responses.
    /// </summary>
    /// <param name="message">The messages from the agent</param>
    /// <param name="context">Workflow context for accessing workflow services and adding events</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
    /// The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        foreach (var msg in message)
        {
            System.Console.WriteLine($"Agent={msg.AuthorName} Text={msg.Text}");
        }

        foreach (var msg in message)
        {
            if (!string.IsNullOrWhiteSpace(msg.Text))
            {
                _messages.Add(msg);
            }
        }

        if (_messages.Count >= 3)
        {
            var kyc = Parse<KycResult>("KYC");
            var fraud = Parse<FraudResult>("Fraud");
            var income = Parse<IncomeResult>("Income");

            var decision = Decide(kyc, fraud, income);
            await context.YieldOutputAsync(
                JsonSerializer.Serialize(decision, JsonOptions),
                cancellationToken
            );
        }
    }

    private T Parse<T>(string agentName) where T : class, new()
    {
        var message = this._messages.LastOrDefault(m => string.Equals(m.AuthorName, agentName, StringComparison.OrdinalIgnoreCase));
        if (message?.Text is null)
        {
            return new T();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(message.Text, JsonOptions) ?? new T();
            var agentProp = typeof(T).GetProperty("Agent");
            if (agentProp is not null)
            {
                var current = agentProp.GetValue(parsed) as string;
                if (string.IsNullOrWhiteSpace(current) || current.StartsWith("functions.", StringComparison.OrdinalIgnoreCase))
                {
                    agentProp.SetValue(parsed, agentName);
                }
            }

            return parsed;
        }
        catch
        {
            return new T();
        }
    }

    private static DecisionResult Decide(KycResult kyc, FraudResult fraud, IncomeResult income)
    {
        var approved = string.Equals(kyc.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(income.Status, "Sufficient", StringComparison.OrdinalIgnoreCase);

        var conditions = new List<string>();
        if (string.Equals(fraud.RiskScore, "Medium", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("Require manual fraud review");
        }
        else if (string.Equals(fraud.RiskScore, "High", StringComparison.OrdinalIgnoreCase))
        {
            approved = false;
        }

        var outcome = approved ? "Approved" : "Rejected";
        var reason = approved
            ? "KYC approved and income sufficient; fraud risk acceptable."
            : "One or more checks failed or require manual review.";

        return new DecisionResult
        {
            Outcome = outcome,
            Conditions = conditions.ToArray(),
            Summary = reason,
            Details = new DecisionDetails
            {
                Kyc = kyc,
                Fraud = fraud,
                Income = income
            }
        };
    }
}
