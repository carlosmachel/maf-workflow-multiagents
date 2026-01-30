using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Console.App.Kycs;

public static class KycAgentFactory
{
    public static ChatClientAgent Create(IChatClient client)
    {
        return new ChatClientAgent(
            client,
            name: "KYC",
            instructions:
            "You validate identity. Use the KycTool to validate the CPF. Return ONLY a JSON object with keys: " +
            "agent (must be exactly \"KYC\"), status (string: Approved|Rejected|Review), notes (string).",
            tools: new List<AITool>() { AIFunctionFactory.Create(KycTools.ValidateCpf)});
    }
}