# Contributing to RE2

Thank you for your interest in contributing to RE2 - the Controlled Drug Licence & GDP Compliance Management System. This guide will help you get started.

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold its standards.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (LTS)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- An IDE such as Visual Studio 2022, JetBrains Rider, or VS Code with the C# Dev Kit
- Git

### Building the Project

```bash
dotnet restore RE2.sln
dotnet build RE2.sln
```

### Running Tests

```bash
dotnet test RE2.sln
```

The test suite covers:

| Project | Scope |
|---|---|
| `RE2.ComplianceCore.Tests` | Domain logic and services |
| `RE2.DataAccess.Tests` | Data access layer |
| `RE2.ComplianceApi.Tests` | API endpoints |
| `RE2.ComplianceFunctions.Tests` | Azure Functions |
| `RE2.ComplianceCli.Tests` | CLI tooling |
| `RE2.Contract.Tests` | API contract/integration tests |

## How to Contribute

### Reporting Issues

- Search existing issues before creating a new one.
- Include steps to reproduce, expected behaviour, and actual behaviour.
- For security vulnerabilities, **do not** open a public issue. Instead, contact the maintainers directly.

### Suggesting Features

- Open an issue describing the use case, not just the solution.
- Explain how the feature relates to licence management or GDP compliance workflows where applicable.

### Submitting Changes

1. **Fork** the repository and create a feature branch from `main`.
2. **Make your changes** following the coding standards below.
3. **Add or update tests** for any new or changed functionality.
4. **Ensure all tests pass** before submitting (`dotnet test RE2.sln`).
5. **Open a pull request** against `main` with a clear description of what and why.

## Coding Standards

### General

- Target **C# 12 / .NET 8**.
- Follow standard [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Keep methods focused and classes cohesive.
- Prefer descriptive names over comments.

### Architecture

- **Stateless design** — no local storage for business data.
- **Composite model** — domain entities span D365 F&O (master data) and Dataverse (compliance extensions). Respect this boundary.
- Use **dependency injection** via `Microsoft.Extensions.DependencyInjection` for all services.
- Use **Entity Framework Core 8** for internal state only, never for business data storage.

### Project Structure

```
src/
  RE2.Shared/              # Shared models and contracts
  RE2.ComplianceCore/      # Domain logic and services
  RE2.DataAccess/          # Data access layer
  RE2.ComplianceApi/       # ASP.NET Core Web API
  RE2.ComplianceWeb/       # Web UI
  RE2.ComplianceFunctions/ # Azure Functions
  RE2.ComplianceCli/       # CLI tool
tests/                     # Mirror of src/ with test projects
infra/                     # Bicep IaC templates
pipelines/                 # Azure DevOps CI/CD pipelines
```

### Testing

- All new features and bug fixes must include tests.
- Use xUnit and follow existing test patterns in the `tests/` directory.
- Aim for meaningful coverage — test behaviour, not implementation details.

### Commits

- Write clear, concise commit messages.
- Use the imperative mood in the subject line (e.g., "Add licence expiry notification" not "Added licence expiry notification").
- Reference related issues where applicable (e.g., `Fixes #42`).

## Pull Request Process

1. Ensure the build passes and all tests are green.
2. Update any relevant documentation if your change affects public APIs or workflows.
3. A maintainer will review your PR. Be prepared to address feedback.
4. Once approved, a maintainer will merge the PR.

## Development Tips

- The solution file `RE2.sln` at the repository root references all projects.
- NuGet package sources are configured in `nuget.config`.
- Infrastructure templates are in `infra/` (Bicep) and deployments are managed through `pipelines/`.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
