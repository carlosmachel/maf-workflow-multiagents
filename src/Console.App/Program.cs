using System.ComponentModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Console.App;

public static class Program
{
    [Description("Know Your Customer by by CPF number")]
    static string KycTool(
        [Description("The CPF formated or unformatted")] string cpf)
    {
        return cpf == "123.456.789-00" ? "Approved" : "Rejected";
    }

    [Description("Assess fraud risk for a credit application")]
    static string FraudTool(
        [Description("The credit application JSON payload")] string applicationJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(applicationJson);
            if (doc.RootElement.TryGetProperty("amount", out var amountProp) &&
                amountProp.TryGetDecimal(out var amount))
            {
                if (amount >= 100_000m)
                {
                    return "High";
                }
                if (amount >= 60_000m)
                {
                    return "Medium";
                }
                return "Low";
            }
        }
        catch
        {
            // Fall through to Review.
        }

        return "Review";
    }

    [Description("Assess income capacity for a credit application")]
    static string IncomeTool(
        [Description("The credit application JSON payload")] string applicationJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(applicationJson);
            if (doc.RootElement.TryGetProperty("amount", out var amountProp) &&
                amountProp.TryGetDecimal(out var amount))
            {
                return amount <= 75_000m ? "Sufficient" : "Insufficient";
            }
        }
        catch
        {
            // Fall through to Review.
        }

        return "Review";
    }

    private static async Task Main()
    {
        DotEnv.Load();
        // Set up the Azure OpenAI client
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";
        var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

        // Create the executors
        ChatClientAgent kyc = new(
            chatClient,
            name: "KYC",
            instructions:
            "You validate identity. Use the KycTool to validate the CPF. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"KYC\"), status (string: Approved|Rejected|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(KycTool) }
        );
        ChatClientAgent fraud = new(
            chatClient,
            name: "Fraud",
            instructions:
            "You assess fraud risk. Use the FraudTool to score the application. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"Fraud\"), riskScore (string: Low|Medium|High|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(FraudTool) }
        );
        ChatClientAgent income = new(
            chatClient,
            name: "Income",
            instructions:
            "You assess income capacity. Use the IncomeTool to score the application. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"Income\"), status (string: Sufficient|Insufficient|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(IncomeTool) }
        );
        var startExecutor = new ConcurrentStartExecutor();
        var aggregationExecutor = new ConcurrentAggregationExecutor();

        // Build the workflow by adding executors and connecting them
        var workflow = new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, [kyc, fraud, income])
            .AddFanInEdge([kyc, fraud, income], aggregationExecutor)
            .WithOutputFrom(aggregationExecutor)
            .Build();

        // Execute the workflow in streaming mode
        await using StreamingRun run = await InProcessExecution.StreamAsync(
            workflow,
            input: "Credit application: {\"amount\":50000,\"currency\":\"BRL\",\"cpf\":\"123.456.789-00\"}"
        );
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case WorkflowOutputEvent output:
                    System.Console.WriteLine($"Workflow completed with results:\n{output.Data}");
                    break;
                case WorkflowErrorEvent error:
                    System.Console.WriteLine($"Workflow error: {error.Exception.Message}");
                    break;
                default:
                    System.Console.WriteLine($"Event: {evt.GetType().Name}");
                    break;
            }
        }
    }
}

/// <summary>
/// Executor that starts the concurrent processing by sending messages to the agents.
/// </summary>
internal sealed class ConcurrentStartExecutor() :
    Executor<string>("ConcurrentStartExecutor")
{
    /// <summary>
    /// Starts the concurrent processing by sending messages to the agents.
    /// </summary>
    /// <param name="message">The user message to process</param>
    /// <param name="context">Workflow context for accessing workflow services and adding events</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
    /// The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Broadcast the message to all connected agents. Receiving agents will queue
        // the message but will not start processing until they receive a turn token.
        await context.SendMessageAsync(new ChatMessage(ChatRole.User, message), cancellationToken: cancellationToken);
        // Broadcast the turn token to kick off the agents.
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);
    }
}

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
                this._messages.Add(msg);
            }
        }

        if (this._messages.Count >= 3)
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

internal sealed class KycResult
{
    public string? Agent { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

internal sealed class FraudResult
{
    public string? Agent { get; set; }
    public string? RiskScore { get; set; }
    public string? Notes { get; set; }
}

internal sealed class IncomeResult
{
    public string? Agent { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

internal sealed class DecisionResult
{
    public string? Outcome { get; set; }
    public string[]? Conditions { get; set; }
    public string? Summary { get; set; }
    public DecisionDetails? Details { get; set; }
}

internal sealed class DecisionDetails
{
    public KycResult? Kyc { get; set; }
    public FraudResult? Fraud { get; set; }
    public IncomeResult? Income { get; set; }
}
