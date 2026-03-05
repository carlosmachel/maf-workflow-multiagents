# Multi-Agent Workflow for Credit Application Assessment

A demonstration project showcasing parallel multi-agent orchestration using Microsoft Agents Framework (MAF) and Azure OpenAI. This application simulates a credit application evaluation process by coordinating multiple AI agents that work concurrently to assess different aspects of a credit request.

## Overview

This project implements a sophisticated workflow that evaluates credit applications through three specialized AI agents running in parallel:

- **KYC Agent**: Validates customer identity (CPF verification)
- **Fraud Agent**: Assesses fraud risk levels
- **Income Agent**: Evaluates income capacity and sufficiency

The workflow uses a fan-out/fan-in pattern to process all validations concurrently and then aggregates the results to make a final credit decision.

## Architecture

```
                    ┌──────────────┐
                    │ Start        │
                    │ Executor     │
                    └──────┬───────┘
                           │
            ┌──────────────┼──────────────┐
            │              │              │
            ▼              ▼              ▼
      ┌─────────┐    ┌─────────┐    ┌─────────┐
      │   KYC   │    │  Fraud  │    │ Income  │
      │  Agent  │    │  Agent  │    │  Agent  │
      └────┬────┘    └────┬────┘    └────┬────┘
            │              │              │
            └──────────────┼──────────────┘
                           │
                    ┌──────▼───────┐
                    │ Aggregation  │
                    │  Executor    │
                    └──────────────┘
```

## Features

- **Concurrent Processing**: All three agents (KYC, Fraud, Income) process the application simultaneously
- **Tool Integration**: Each agent uses specialized tools to perform their assessments
- **Event Streaming**: Real-time workflow event monitoring
- **Automatic Aggregation**: Results are aggregated and a final decision is made
- **Azure OpenAI Integration**: Powered by Azure OpenAI GPT models

## Technology Stack

- **.NET 10.0**: Latest .NET framework
- **Microsoft Agents Framework (MAF)**: Agent orchestration and workflow management
- **Azure OpenAI**: AI model provider
- **Azure Identity**: Authentication with Azure CLI credentials
- **dotenv.net**: Environment variable management

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure subscription with OpenAI service enabled
- Azure CLI (authenticated)

## Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd maf-workflow-multiagents
   ```

2. **Configure environment variables**
   
   Create a `.env` file in the `src/Console.App` directory:
   ```env
   AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4.1-mini
   ```

3. **Authenticate with Azure CLI**
   ```bash
   az login
   ```

4. **Restore dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

## Usage

Run the application:
```bash
cd src/Console.App
dotnet run
```

The application will process a sample credit application:
```json
{
  "amount": 50000,
  "currency": "BRL",
  "cpf": "123.456.789-00"
}
```

### Sample Output

The workflow will emit events showing:
- Agent execution progress
- Individual agent assessments
- Final aggregated decision

Example:
```
Agent=KYC Text={"agent":"KYC","status":"Approved","notes":"CPF validated successfully"}
Agent=Fraud Text={"agent":"Fraud","riskScore":"Low","notes":"No fraud indicators detected"}
Agent=Income Text={"agent":"Income","status":"Sufficient","notes":"Income adequate for requested amount"}
Workflow completed with results:
{"decision":"APPROVED","kyc":{"agent":"KYC","status":"Approved",...},...}
```

## Project Structure

```
src/Console.App/
├── Program.cs                    # Application entry point
├── WorkflowFactory.cs            # Workflow configuration and setup
├── Executors/
│   ├── ConcurrentStartExecutor.cs       # Initiates parallel processing
│   ├── ConcurrentAggregationExecutor.cs # Aggregates agent results
│   └── DecisionResult.cs                # Final decision model
├── Kycs/
│   ├── KycAgentFactory.cs        # KYC agent configuration
│   ├── KycTool.cs                # CPF validation tool
│   └── KycResult.cs              # KYC result model
├── Frauds/
│   ├── FraudAgentFactory.cs      # Fraud agent configuration
│   ├── FraudTools.cs             # Fraud assessment tool
│   └── FraudResult.cs            # Fraud result model
└── Incomes/
    ├── IncomeAgentFactory.cs     # Income agent configuration
    ├── IncomeTools.cs            # Income verification tool
    └── IncomeResult.cs           # Income result model
```

## How It Works

1. **Workflow Initialization**: The `WorkflowFactory` creates all agents and executors, connecting them in a fan-out/fan-in pattern

2. **Request Distribution**: The `ConcurrentStartExecutor` broadcasts the credit application to all three agents simultaneously

3. **Parallel Processing**: Each agent:
   - Receives the application data
   - Invokes its specialized tool
   - Returns a structured JSON response

4. **Result Aggregation**: The `ConcurrentAggregationExecutor`:
   - Collects responses from all agents
   - Parses individual results
   - Makes a final credit decision based on all assessments

5. **Decision Logic**:
   - **APPROVED**: KYC approved, fraud risk low/medium, income sufficient
   - **REJECTED**: Any agent returns rejection/high risk/insufficient status
   - **REVIEW**: Any agent returns review status

## Customization

### Adding New Agents

1. Create a new folder for your agent (e.g., `Compliance/`)
2. Implement:
   - `AgentFactory.cs`: Agent configuration
   - `Tools.cs`: Agent-specific tools
   - `Result.cs`: Result model
3. Register in `WorkflowFactory.cs`

### Modifying Decision Logic

Edit the aggregation logic in `ConcurrentAggregationExecutor.cs` to implement your custom business rules.

## Dependencies

Key packages:
- `Microsoft.Agents.AI` (1.0.0-preview.260121.1)
- `Microsoft.Agents.AI.Workflows` (1.0.0-preview.260121.1)
- `Azure.AI.OpenAI` (2.8.0-beta.1)
- `Azure.Identity` (1.18.0-beta.2)

## License

This project is licensed under the terms specified in the LICENSE file.

## Contributing

This is a demonstration project for presentations/talks. Feel free to fork and adapt for your own use cases.

## Notes

- This is a **demonstration project** intended for educational purposes
- The CPF validation and fraud detection are simulated
- In production, integrate with real validation services and databases
- Consider implementing proper error handling, logging, and monitoring

## Resources

- [Microsoft Agents Framework Documentation](https://learn.microsoft.com/en-us/azure/ai-services/agents/)
- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Detailed Architecture

### Components Overview

**Entry Point (`Program.cs`)**
- Loads environment variables from `.env` file
- Creates Azure OpenAI client using `AzureCliCredential` for authentication
- Initializes and runs the workflow in streaming mode (`InProcessExecution.StreamAsync`)

**Workflow Factory (`WorkflowFactory.cs`)**
- Builds the complete workflow by connecting executors:
  - `ConcurrentStartExecutor`: Initiates processing, broadcasts messages and turn tokens
  - Agents (KYC, Fraud, Income): Process the application in parallel
  - `ConcurrentAggregationExecutor`: Aggregates agent responses and emits final result

### Agent Details

#### KYC Agent
- **Factory**: `src/Console.App/Kycs/KycAgentFactory.cs`
- **Tool**: `src/Console.App/Kycs/KycTool.cs`
- **Behavior**: Validates Brazilian CPF (tax ID)
- **Returns**: JSON with keys: `agent="KYC"`, `status` (Approved|Rejected|Review), `notes`
- **Validation Logic**:
  - CPF `123.456.789-00` → `Rejected`
  - Any other CPF → `Approved`

#### Fraud Agent
- **Factory**: `src/Console.App/Frauds/FraudAgentFactory.cs`
- **Tool**: `src/Console.App/Frauds/FraudTools.cs`
- **Behavior**: Assesses fraud risk based on requested amount
- **Returns**: JSON with keys: `agent="Fraud"`, `riskScore` (Low|Medium|High|Review), `notes`
- **Risk Assessment**:
  - `amount >= 100,000` → `High` risk
  - `amount >= 60,000` → `Medium` risk
  - `amount < 60,000` → `Low` risk
  - Parse error → `Review`

#### Income Agent
- **Factory**: `src/Console.App/Incomes/IncomeAgentFactory.cs`
- **Tool**: `src/Console.App/Incomes/IncomeTools.cs`
- **Behavior**: Evaluates payment capacity
- **Returns**: JSON with keys: `agent="Income"`, `status` (Sufficient|Insufficient|Review), `notes`
- **Capacity Assessment**:
  - `amount <= 75,000` → `Sufficient`
  - `amount > 75,000` → `Insufficient`
  - Parse error → `Review`

### Aggregation and Decision Logic

**Executor**: `src/Console.App/Executors/ConcurrentAggregationExecutor.cs`

**Process**:
1. Collects JSON messages from all agents
2. Parses and normalizes the `Agent` property when needed
3. Applies decision rules via the `Decide()` method

**Decision Rules**:
- **Initial Approval Criteria**:
  - `KYC.Status == "Approved"` AND `Income.Status == "Sufficient"`
- **Fraud Risk Effects**:
  - `Medium` → Adds condition `"Require manual fraud review"` (does not auto-reject)
  - `High` → Forces rejection
- **Result Model** (`DecisionResult`):
  - `Outcome`: `"Approved"` or `"Rejected"`
  - `Conditions`: Array of strings (e.g., `["Require manual fraud review"]`)
  - `Summary`: Brief reasoning
  - `Details`: Contains `Kyc`, `Fraud`, `Income` objects returned by agents

**Result Class**: `src/Console.App/Executors/DecisionResult.cs`

## Input Format

The application expects a string containing the credit application JSON.

**Example** (used in `Program.cs`):
```json
Credit application: {"amount":50000,"currency":"BRL","cpf":"123.456.789-00"}
```

## Expected Output Examples

### Example 1: Approved Application
**Scenario**: KYC approved, sufficient income, low fraud risk

```json
{
  "Outcome": "Approved",
  "Conditions": [],
  "Summary": "KYC approved and income sufficient; fraud risk acceptable.",
  "Details": {
    "Kyc": { "Agent": "KYC", "Status": "Approved", "Notes": "" },
    "Fraud": { "Agent": "Fraud", "RiskScore": "Low", "Notes": "" },
    "Income": { "Agent": "Income", "Status": "Sufficient", "Notes": "" }
  }
}
```

### Example 2: Approved with Conditions
**Scenario**: KYC approved, sufficient income, medium fraud risk

```json
{
  "Outcome": "Approved",
  "Conditions": ["Require manual fraud review"],
  "Summary": "KYC approved and income sufficient; fraud risk acceptable.",
  "Details": {
    "Kyc": { "Agent": "KYC", "Status": "Approved", "Notes": "" },
    "Fraud": { "Agent": "Fraud", "RiskScore": "Medium", "Notes": "" },
    "Income": { "Agent": "Income", "Status": "Sufficient", "Notes": "" }
  }
}
```

### Example 3: Rejected Application
**Scenario**: KYC rejected or insufficient income

```json
{
  "Outcome": "Rejected",
  "Conditions": [],
  "Summary": "One or more checks failed or require manual review.",
  "Details": {
    "Kyc": { "Agent": "KYC", "Status": "Rejected", "Notes": "" },
    "Fraud": { "Agent": "Fraud", "RiskScore": "Low", "Notes": "" },
    "Income": { "Agent": "Income", "Status": "Sufficient", "Notes": "" }
  }
}
```

## Environment Variables

Required and optional environment variables:

- **`AZURE_OPENAI_ENDPOINT`** (required) — Azure OpenAI resource URL (e.g., `https://my-openai.openai.azure.com`)
- **`AZURE_OPENAI_DEPLOYMENT_NAME`** (optional) — Deployment/model name; default in code: `gpt-4.1-mini`

**Example `.env` file**:
```env
AZURE_OPENAI_ENDPOINT=https://my-openai.openai.azure.com
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4.1-mini
```

## Running the Application

### Basic Commands

From the repository root:
```bash
cd src/Console.App
dotnet run
```

Or explicitly specifying the project:
```bash
dotnet run --project src/Console.App/Console.App.csproj
```

### Important Notes

- The application uses `AzureCliCredential` for authentication — run `az login` before executing
- To use environment variables instead of Azure CLI, modify the code to use another compatible credential (e.g., `DefaultAzureCredential`) or configure according to Azure documentation

## Logs and Output

- The application prints workflow events while running (`Program.cs` — `WatchStreamAsync`)
- `ConcurrentAggregationExecutor` writes each received message with:
  ```csharp
  System.Console.WriteLine($"Agent={msg.AuthorName} Text={msg.Text}");
  ```

## Extension Points

### Adding New Agents

To add a new verification agent:

1. Create a similar factory in `src/Console.App` (e.g., `NewAgent/NewAgentFactory.cs`) that returns `ChatClientAgent`
2. Create the tools (annotated static methods) to expose functions to the agent (`AIFunctionFactory.Create`)
3. Update `WorkflowFactory.Create` to include the new agent in the fan-out/fan-in collections
4. Update `ConcurrentAggregationExecutor.Decide` and the `DecisionResult` model if you need to include new details

### Modifying Decision Logic

Edit the decision logic in `ConcurrentAggregationExecutor.Decide` method.

## Troubleshooting

Common errors and solutions:

- **Error `AZURE_OPENAI_ENDPOINT is not set.`**: Check your `.env` file and/or environment variables
- **Credential error with `AzureCliCredential`**: Run `az login` and confirm your account has access to the OpenAI resource
- **JSON parsing from agents**: If an agent returns free text (not valid JSON), the aggregator falls back to an empty object — review agent instructions to enforce strict JSON output

## Key Files Summary

- `src/Console.App/Program.cs` — Client initialization and workflow execution
- `src/Console.App/WorkflowFactory.cs` — Workflow assembly (fan-out/fan-in)
- `src/Console.App/Kycs/*` — KYC agent and tool
- `src/Console.App/Frauds/*` — Fraud agent and tool
- `src/Console.App/Incomes/*` — Income agent and tool
- `src/Console.App/Executors/*` — Executors (start, aggregation, DecisionResult)
