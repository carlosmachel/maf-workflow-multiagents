using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Console.App;

public static class Program
{
    private static async Task Main()
    {
        DotEnv.Load();
        // Set up the Azure OpenAI client
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";
        var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

        var workflow = WorkflowFactory.Create(chatClient);
        
        await using var run = await InProcessExecution.StreamAsync(
            workflow,
            input: "Credit application: {\"amount\":50000,\"currency\":\"BRL\",\"cpf\":\"123.456.789-00\"}"
        );
        await foreach (var evt in run.WatchStreamAsync())
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


