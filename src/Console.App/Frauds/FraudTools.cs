using System.ComponentModel;
using System.Text.Json;

namespace Console.App.Frauds;

public static class FraudTools
{
    [Description("Assess fraud risk for a credit application")]
    public static string FraudTool(
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

}