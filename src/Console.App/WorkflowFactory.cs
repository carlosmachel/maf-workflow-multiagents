using Console.App.Executors;
using Console.App.Frauds;
using Console.App.Incomes;
using Console.App.Kycs;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Console.App;

public class WorkflowFactory
{
    public static Workflow Create(IChatClient chatClient)
    {
        // Create the executors
        var kyc = KycAgentFactory.Create(chatClient);
        var fraud = FraudAgentFactory.Create(chatClient);
        var income = IncomeAgentFactory.Create(chatClient);
        var startExecutor = new ConcurrentStartExecutor();
        var aggregationExecutor = new ConcurrentAggregationExecutor();

        // Build the workflow by adding executors and connecting them
        var workflow = new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, [kyc, fraud, income])
            .AddFanInEdge([kyc, fraud, income], aggregationExecutor)
            .WithOutputFrom(aggregationExecutor)
            .Build();

        return workflow;
    }
}