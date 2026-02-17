# RE2 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-09

## Active Technologies
- C# 12 / .NET 8 LTS (established) + ASP.NET Core 8.0, Microsoft.Extensions.* (established) (main)
- Dataverse virtual tables for new entities (`phr_gdpcredential`, `phr_gdpserviceprovider`, `phr_qualificationreview`, `phr_gdpcredentialverification`) (main)
- C# 12 / .NET 8 LTS + ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Entity Framework Core 8.0 (internal state only) (main)
- Dataverse virtual tables (`phr_gdpinspection`, `phr_gdpinspectionfinding`, `phr_capa`) (main)
- C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL) + ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Azure SDK libraries (Azure.Identity, Azure.Storage.Blobs), Azure.Functions.Worker, Microsoft.PowerPlatform.Dataverse.Client 1.0.x (main)
- Dataverse virtual tables (phr_gdpdocument), Azure Blob Storage for document files (via existing IDocumentStorage/DocumentStorageClient) (main)

- C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL) + ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Azure SDK libraries (Azure.Identity, Azure.Storage.Blobs, Azure.Messaging.ServiceBus), Entity Framework Core 8.0 (for internal state only - NOT for business data storage) (001-licence-management)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL)

## Code Style

C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL): Follow standard conventions

## Recent Changes
- main: Added C# 12 / .NET 8 LTS (Long-Term Support, November 2026 EOL) + ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Azure SDK libraries (Azure.Identity, Azure.Storage.Blobs), Azure.Functions.Worker, Microsoft.PowerPlatform.Dataverse.Client 1.0.x
- main: Added C# 12 / .NET 8 LTS + ASP.NET Core 8.0, Microsoft.Extensions.* (DI, Configuration, Logging), Entity Framework Core 8.0 (internal state only)
- main: Added C# 12 / .NET 8 LTS (established) + ASP.NET Core 8.0, Microsoft.Extensions.* (established)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
