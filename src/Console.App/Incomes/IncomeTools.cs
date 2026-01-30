using System.ComponentModel;
using System.Text.Json;

namespace Console.App.Incomes;

public static class IncomeTools
{
    [Description("Assess income capacity for a credit application")]
    public static string IncomeTool(
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

}