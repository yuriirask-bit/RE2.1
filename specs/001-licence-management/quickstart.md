# Developer Quickstart Guide

**Feature**: Controlled Drug Licence & GDP Compliance Management
**Branch**: `001-licence-management`
**Date**: 2026-01-09

## Overview

This quickstart guide helps developers set up their local development environment and understand the architecture for building the Controlled Drug Licence & GDP Compliance Management system.

## Prerequisites

### Required Software

- **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (17.8+) or **VS Code** with C# extension
- **Azure CLI**: [Install guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- **Git**: Version control
- **Docker Desktop**: For running TestContainers during integration tests

### Azure Subscriptions & Access

- Azure subscription with permissions to create:
  - App Service and App Service Plans
  - Azure Functions
  - Azure API Management (Developer tier for dev)
  - Azure Storage Account (Blob Storage)
  - Application Insights
  - Azure Cache for Redis (optional)
- Access to **Dataverse environment** (provided by platform team)
- Access to **D365 Finance & Operations environment** (provided by platform team)

### Service Principal / Managed Identity

For local development, you'll need a service principal with permissions to:
- Read/write Dataverse virtual tables (`re2_*` entities)
- Read D365 F&O virtual data entities (Customers, Vendors)
- Read/write Azure Blob Storage

Contact your platform team for service principal credentials.

## Repository Setup

### Clone and Branch

```bash
git clone <repository-url>
cd RE2
git checkout 001-licence-management
```

### Project Structure

```
src/
├── RE2.ComplianceCore/       # Core business logic library
├── RE2.DataAccess/           # External system API clients
├── RE2.ComplianceApi/        # ASP.NET Core Web API
├── RE2.ComplianceWeb/        # ASP.NET Core MVC Web UI
├── RE2.ComplianceFunctions/  # Azure Functions (background jobs)
└── RE2.Shared/               # Shared utilities

tests/
├── RE2.ComplianceCore.Tests/
├── RE2.ComplianceApi.Tests/
├── RE2.DataAccess.Tests/
└── RE2.Contract.Tests/
```

### Restore Dependencies

```bash
dotnet restore src/RE2.sln
```

## Configuration

### Local Development Settings

Create `appsettings.Development.json` in each project:

#### RE2.ComplianceApi and RE2.ComplianceWeb

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "RE2": "Debug"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ClientSecret": "<your-client-secret>"
  },
  "Dataverse": {
    "BaseUrl": "https://<your-org>.crm4.dynamics.com/api/data/v9.2",
    "ClientId": "<service-principal-client-id>",
    "ClientSecret": "<service-principal-secret>",
    "TenantId": "<tenant-id>"
  },
  "D365FinanceOperations": {
    "BaseUrl": "https://<your-instance>.operations.dynamics.com/data",
    "ClientId": "<service-principal-client-id>",
    "ClientSecret": "<service-principal-secret>",
    "TenantId": "<tenant-id>"
  },
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "licence-documents-dev"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "RE2Dev:"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

**Security Note**: NEVER commit `appsettings.Development.json` with secrets. Use Azure Key Vault or User Secrets for production.

#### RE2.ComplianceFunctions

Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DataverseBaseUrl": "https://<your-org>.crm4.dynamics.com/api/data/v9.2",
    "DataverseClientId": "<service-principal-client-id>",
    "DataverseClientSecret": "<service-principal-secret>",
    "DataverseTenantId": "<tenant-id>",
    "BlobStorageConnectionString": "UseDevelopmentStorage=true",
    "APPINSIGHTS_INSTRUMENTATIONKEY": ""
  }
}
```

### User Secrets (Recommended for Local Dev)

Instead of storing secrets in files, use .NET User Secrets:

```bash
cd src/RE2.ComplianceApi
dotnet user-secrets init
dotnet user-secrets set "Dataverse:ClientSecret" "your-secret-here"
dotnet user-secrets set "D365FinanceOperations:ClientSecret" "your-secret-here"
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here"
```

## Running Locally

### Start Azurite (Local Azure Storage Emulator)

```bash
# Install Azurite if not already installed
npm install -g azurite

# Start Azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

### (Optional) Start Local Redis

```bash
docker run -d -p 6379:6379 --name redis-dev redis:latest
```

### Run API Project

```bash
cd src/RE2.ComplianceApi
dotnet run
```

API will be available at `https://localhost:7001` (HTTPS) and `http://localhost:5001` (HTTP).

### Run Web UI Project

```bash
cd src/RE2.ComplianceWeb
dotnet run
```

Web UI will be available at `https://localhost:7002`.

### Run Azure Functions Locally

```bash
cd src/RE2.ComplianceFunctions
func start
```

Timer triggers won't fire automatically during local development. Use:

```bash
# Manually trigger a function
curl http://localhost:7071/admin/functions/LicenceExpiryMonitor -d '{}'
```

## Running Tests

### Unit Tests (Fast)

```bash
dotnet test tests/RE2.ComplianceCore.Tests/ --filter "Category=Unit"
```

### Integration Tests (Requires TestContainers)

```bash
# Start Docker Desktop first
dotnet test tests/RE2.ComplianceApi.Tests/ --filter "Category=Integration"
```

### Contract Tests

```bash
dotnet test tests/RE2.Contract.Tests/
```

### All Tests

```bash
dotnet test src/RE2.sln
```

## CLI Interface (Constitution Principle IV)

The RE2.ComplianceCli provides a text-based interface for debugging, scripting, and automation. All commands output JSON to stdout for programmatic consumption.

### Build and Run CLI

```bash
# Build CLI
dotnet build src/RE2.ComplianceCli/RE2.ComplianceCli.csproj

# Run CLI (shows available commands)
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- --help
```

### Available Commands

#### 1. validate-transaction

Validates a transaction against compliance rules. Accepts JSON via stdin or file.

**Input**: Transaction JSON with customer, substances, and quantities.
**Output**: Validation result with violations and licence coverage.

```bash
# From file
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- validate-transaction --file transaction.json

# From stdin (pipe)
echo '{"customerId":"00000000-0000-0000-0000-000000000010","transactionType":"Order","transactionDirection":"Outbound","lines":[{"substanceId":"00000000-0000-0000-0000-000000000001","quantity":100}]}' | dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- validate-transaction
```

**Exit Codes**:
- `0`: Transaction valid
- `1`: Error (invalid input, service error)
- `2`: Validation failed (compliance violations)

#### 2. lookup-customer

Retrieves customer compliance status by ID or business name.

```bash
# By ID
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-customer --id 00000000-0000-0000-0000-000000000010

# By name (partial match)
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-customer --name "Amsterdam"

# Include associated licences
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-customer --id 00000000-0000-0000-0000-000000000010 --include-licences
```

**Output**: Customer details with compliance status (canTransact, isSuspended, warnings).

#### 3. lookup-licence

Retrieves licence details by licence number or ID.

```bash
# By licence number
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-licence --number WDA-NL-2024-001

# By ID
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-licence --id 00000000-0000-0000-0000-000000000001

# Include substance mappings
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-licence --number WDA-NL-2024-001 --include-substances

# Include supporting documents
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-licence --number WDA-NL-2024-001 --include-documents
```

**Output**: Licence details including status, expiry, permitted activities, and optionally substance mappings.

#### 4. generate-report

Generates compliance reports in JSON format.

```bash
# Customer compliance report
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- generate-report -t customer-compliance

# Expiring licences report (default: 90 days ahead)
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- generate-report -t expiring-licences --days 60

# Alerts summary report
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- generate-report -t alerts-summary

# Transaction history report with filters
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- generate-report -t transaction-history --customer-id 00000000-0000-0000-0000-000000000010 --from 2026-01-01 --to 2026-01-31

# Output to file
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- generate-report -t customer-compliance -o compliance-report.json
```

**Report Types**:
- `customer-compliance`: Summary of all customers' compliance status
- `expiring-licences`: Licences expiring within specified days
- `alerts-summary`: Active alert counts by type and severity
- `transaction-history`: Transaction validation history

### CLI Output Format

All CLI commands output JSON to stdout. Error responses follow this format:

```json
{
  "error": "Error message description",
  "errorType": "ValidationError"
}
```

Success responses vary by command but always include relevant entity data.

### Verbose Mode

Add `--verbose` to any command for detailed logging to stderr:

```bash
dotnet run --project src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -- lookup-customer --id 00000000-0000-0000-0000-000000000010 --verbose
```

### Building Standalone Executable

For deployment without .NET SDK:

```bash
# Windows self-contained
dotnet publish src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -c Release -r win-x64 --self-contained -o ./publish/cli-win

# Linux self-contained
dotnet publish src/RE2.ComplianceCli/RE2.ComplianceCli.csproj -c Release -r linux-x64 --self-contained -o ./publish/cli-linux

# Run standalone
./publish/cli-win/RE2.ComplianceCli.exe lookup-customer --name "Amsterdam"
```

## TDD Workflow

Per the SpecKit constitution (Principle II: Test-First Development), follow this workflow:

### 1. Write Test First (Red)

```csharp
// tests/RE2.ComplianceCore.Tests/Services/LicenceValidationServiceTests.cs
[Fact]
public void ValidateLicence_WhenExpired_ShouldReturnInvalid()
{
    // Arrange
    var mockClient = new Mock<IDataverseClient>();
    var service = new LicenceValidationService(mockClient.Object);
    var licence = new Licence
    {
        ExpiryDate = DateTime.UtcNow.AddDays(-1),
        Status = LicenceStatus.Expired
    };

    // Act
    var result = service.ValidateLicence(licence);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Reason.Should().Contain("expired");
}
```

### 2. Run Test and Verify It Fails

```bash
dotnet test tests/RE2.ComplianceCore.Tests/ --filter "FullyQualifiedName~ValidateLicence_WhenExpired"
# Expected: Test fails (no implementation yet)
```

### 3. Implement Minimum Code to Pass (Green)

```csharp
// src/RE2.ComplianceCore/Services/LicenceValidation/LicenceValidationService.cs
public class LicenceValidationService : ILicenceValidationService
{
    public ValidationResult ValidateLicence(Licence licence)
    {
        if (licence.ExpiryDate < DateTime.UtcNow || licence.Status == LicenceStatus.Expired)
        {
            return ValidationResult.Invalid("Licence has expired");
        }

        return ValidationResult.Valid();
    }
}
```

### 4. Run Test and Verify It Passes

```bash
dotnet test tests/RE2.ComplianceCore.Tests/ --filter "FullyQualifiedName~ValidateLicence_WhenExpired"
# Expected: Test passes (green)
```

### 5. Refactor (While Keeping Tests Green)

Improve code quality, extract methods, add abstractions - but keep all tests passing.

## Key Architecture Patterns

### Stateless Service Design

**No local data storage** for business entities. All data fetched from Dataverse/D365 via API calls:

```csharp
public class TransactionValidationService
{
    private readonly IDataverseClient _dataverseClient;
    private readonly ID365FoClient _d365Client;

    public async Task<ValidationResult> ValidateTransactionAsync(TransactionRequest request)
    {
        // Fetch customer data from D365 F&O
        var customer = await _d365Client.GetCustomerAsync(request.CustomerId);

        // Fetch licences from Dataverse
        var licences = await _dataverseClient.GetLicencesForCustomerAsync(request.CustomerId);

        // Perform validation (pure logic, no data persistence)
        return PerformValidation(customer, licences, request);
    }
}
```

### Dependency Injection Setup

```csharp
// Program.cs
builder.Services.AddScoped<IDataverseClient, DataverseClient>();
builder.Services.AddScoped<ID365FoClient, D365FoClient>();
builder.Services.AddScoped<ILicenceValidationService, LicenceValidationService>();
builder.Services.AddScoped<ITransactionComplianceService, TransactionComplianceService>();

// Configure HttpClients with authentication
builder.Services.AddHttpClient<IDataverseClient, DataverseClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Dataverse:BaseUrl"]);
});
```

### Optimistic Concurrency

All mutable entities include a `Version` field for conflict detection (FR-027a):

```csharp
public class Licence
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; }
    public int Version { get; set; }  // Optimistic concurrency token
    // ... other properties
}

// Update with version check
public async Task UpdateLicenceAsync(Licence licence)
{
    var response = await _httpClient.PatchAsync(
        $"re2_licences({licence.LicenceId})",
        CreateJsonContent(licence),
        new Dictionary<string, string> { ["If-Match"] = $"\"{licence.Version}\"" }
    );

    if (response.StatusCode == HttpStatusCode.PreconditionFailed)
    {
        throw new ConcurrencyConflictException("Licence was modified by another user");
    }
}
```

## Testing External API Dependencies

### Mock Dataverse Client (Unit Tests)

```csharp
var mockDataverseClient = new Mock<IDataverseClient>();
mockDataverseClient
    .Setup(c => c.GetLicencesForCustomerAsync(It.IsAny<Guid>()))
    .ReturnsAsync(new List<Licence> { /* test data */ });
```

### TestContainers (Integration Tests)

```csharp
public class TransactionValidationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly MockDataverseServer _mockDataverse;

    public TransactionValidationApiTests(WebApplicationFactory<Program> factory)
    {
        // Start mock server container
        _mockDataverse = new MockDataverseServer();
        _mockDataverse.Start();

        // Configure test app to use mock server
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dataverse:BaseUrl"] = _mockDataverse.BaseUrl
                });
            });
        }).CreateClient();
    }
}
```

## Common Issues & Solutions

### Issue: "Unauthorized" when calling Dataverse

**Solution**: Verify service principal has correct permissions:
```bash
az ad sp show --id <client-id>
# Contact platform team to grant permissions: Dynamics CRM API -> user_impersonation
```

### Issue: TestContainers fail on Windows

**Solution**: Ensure Docker Desktop is running and WSL2 backend is enabled.

### Issue: Azure Functions won't start locally

**Solution**: Install Azure Functions Core Tools:
```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### Issue: Azurite blob storage connection fails

**Solution**: Reset Azurite:
```bash
azurite --silent --location c:\azurite --blobPort 10000 --clean
```

## Next Steps

1. **Read** `data-model.md` to understand entity relationships
2. **Review** `contracts/transaction-validation-api.yaml` to understand API contracts
3. **Explore** `research.md` for technology decisions and best practices
4. **Check** `plan.md` for overall architecture and constitution compliance
5. **Start** with User Story 1 (Manage Legal Licence Requirements) - see `tasks.md` (generated by `/speckit.tasks`)

## Useful Commands

```bash
# Watch tests (auto-run on file changes)
dotnet watch test tests/RE2.ComplianceCore.Tests/

# Format code
dotnet format src/RE2.sln

# Build release
dotnet build src/RE2.sln --configuration Release

# Publish API for deployment
dotnet publish src/RE2.ComplianceApi/ -c Release -o ./publish/api

# Deploy to Azure (via Bicep)
az deployment group create \
  --resource-group rg-re2-dev \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev

# View logs from Azure App Service
az webapp log tail --name re2-compliance-api-dev --resource-group rg-re2-dev
```

## Support & Resources

- **Architecture Questions**: See `plan.md` and `research.md`
- **API Contracts**: See `contracts/*.yaml`
- **Data Model**: See `data-model.md`
- **Azure Docs**: [App Service](https://learn.microsoft.com/en-us/azure/app-service/), [Functions](https://learn.microsoft.com/en-us/azure/azure-functions/)
- **Dataverse Docs**: [Web API Reference](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview)
- **D365 F&O Docs**: [Virtual Entities](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/data-entities/data-entities)
