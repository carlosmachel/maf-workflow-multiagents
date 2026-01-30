using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Console.App.Frauds;

public static class FraudAgentFactory
{
    public static ChatClientAgent Create(IChatClient client)
    {
        return new(
            client,
            name: "Fraud",
            instructions:
            "You assess fraud risk. Use the FraudTool to score the application. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"Fraud\"), riskScore (string: Low|Medium|High|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(FraudTools.FraudTool) }
        );
    }
}