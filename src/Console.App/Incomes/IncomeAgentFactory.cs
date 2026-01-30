using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Console.App.Incomes;

public static class IncomeAgentFactory
{

    public static ChatClientAgent Create(IChatClient client)
    {
        return new(
            client,
            name: "Income",
            instructions:
            "You assess income capacity. Use the IncomeTool to score the application. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"Income\"), status (string: Sufficient|Insufficient|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(IncomeTools.IncomeTool) }
        );
    }
}