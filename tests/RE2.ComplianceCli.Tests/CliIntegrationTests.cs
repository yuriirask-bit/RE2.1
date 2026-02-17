using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RE2.ComplianceCli.Tests;

/// <summary>
/// T052h: CLI integration tests verifying stdin/stdout protocol.
/// Tests the CLI executable end-to-end to ensure proper text I/O per Constitution Principle IV.
/// Customer lookup uses composite key: --account + --data-area instead of --id (Guid).
/// </summary>
public class CliIntegrationTests
{
    private readonly string _cliProjectPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public CliIntegrationTests()
    {
        // Navigate from test project to CLI project
        var testDir = AppContext.BaseDirectory;
        _cliProjectPath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "..", "src", "RE2.ComplianceCli", "RE2.ComplianceCli.csproj"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Generate Report Command Tests

    [Fact]
    public async Task GenerateReport_CustomerCompliance_ReturnsValidJson()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("generate-report", "-t", "customer-compliance");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stdout));

        var json = JsonDocument.Parse(stdout);
        Assert.Equal("customer-compliance", json.RootElement.GetProperty("reportType").GetString());
        Assert.True(json.RootElement.TryGetProperty("summary", out _));
        Assert.True(json.RootElement.TryGetProperty("customers", out _));
    }

    [Fact]
    public async Task GenerateReport_ExpiringLicences_ReturnsValidJson()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("generate-report", "-t", "expiring-licences");

        // Assert
        Assert.Equal(0, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.Equal("expiring-licences", json.RootElement.GetProperty("reportType").GetString());
        Assert.True(json.RootElement.TryGetProperty("summary", out _));
        Assert.True(json.RootElement.TryGetProperty("licences", out _));
    }

    [Fact]
    public async Task GenerateReport_AlertsSummary_ReturnsValidJson()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("generate-report", "-t", "alerts-summary");

        // Assert
        Assert.Equal(0, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.Equal("alerts-summary", json.RootElement.GetProperty("reportType").GetString());
        Assert.True(json.RootElement.TryGetProperty("summary", out _));
    }

    [Fact]
    public async Task GenerateReport_InvalidType_ReturnsError()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("generate-report", "-t", "invalid-report-type");

        // Assert
        Assert.Equal(1, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("error", out _));
    }

    #endregion

    #region Lookup Customer Command Tests

    [Fact]
    public async Task LookupCustomer_ByAccount_ReturnsValidJson()
    {
        // Use a known seed data customer account with data area
        var customerAccount = "CUST-001";
        var dataAreaId = "nlpd";

        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-customer", "--account", customerAccount, "--data-area", dataAreaId);

        // Assert
        Assert.Equal(0, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("customerAccount", out _));
        Assert.True(json.RootElement.TryGetProperty("dataAreaId", out _));
        Assert.True(json.RootElement.TryGetProperty("complianceStatus", out _));
    }

    [Fact]
    public async Task LookupCustomer_ByName_ReturnsValidJson()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-customer", "--name", "Amsterdam");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stdout));

        // Should return either a single customer or a list of matches
        var json = JsonDocument.Parse(stdout);
        var hasCustomerAccount = json.RootElement.TryGetProperty("customerAccount", out _);
        var hasMatches = json.RootElement.TryGetProperty("matches", out _);
        Assert.True(hasCustomerAccount || hasMatches);
    }

    [Fact]
    public async Task LookupCustomer_NotFound_ReturnsError()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-customer", "--account", "NONEXISTENT-999", "--data-area", "nlpd");

        // Assert
        Assert.Equal(1, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task LookupCustomer_NoAccountOrName_ReturnsError()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-customer");

        // Assert
        Assert.Equal(1, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("error", out _));
    }

    #endregion

    #region Lookup Licence Command Tests

    [Fact]
    public async Task LookupLicence_ByNumber_ReturnsValidJson()
    {
        // Use a known seed data licence number
        var licenceNumber = "WDA-NL-2024-001";

        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-licence", "--number", licenceNumber);

        // Assert
        Assert.Equal(0, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("licenceId", out _));
        Assert.True(json.RootElement.TryGetProperty("licenceNumber", out _));
        Assert.True(json.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task LookupLicence_NotFound_ReturnsError()
    {
        // Act
        var (exitCode, stdout, _) = await RunCliAsync("lookup-licence", "--number", "NONEXISTENT-12345");

        // Assert
        Assert.Equal(1, exitCode);

        var json = JsonDocument.Parse(stdout);
        Assert.True(json.RootElement.TryGetProperty("error", out _));
    }

    #endregion

    #region Validate Transaction Command Tests

    [Fact]
    public async Task ValidateTransaction_ValidJson_ReturnsValidationResult()
    {
        // Arrange - create a sample transaction JSON using customer account + data area
        var transactionJson = JsonSerializer.Serialize(new
        {
            customerAccount = "CUST-001",
            dataAreaId = "nlpd",
            transactionType = "Order",
            transactionDirection = "Outbound",
            destinationCountry = "NL",
            lines = new[]
            {
                new
                {
                    substanceId = "00000000-0000-0000-0000-000000000001",
                    quantity = 100.0m,
                    unitOfMeasure = "g"
                }
            }
        }, _jsonOptions);

        // Create a temp file with the transaction JSON
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, transactionJson);

            // Act
            var (exitCode, stdout, _) = await RunCliAsync("validate-transaction", "--file", tempFile);

            // Assert - exit code 0 for valid, 2 for validation failures
            Assert.True(exitCode == 0 || exitCode == 2);

            var json = JsonDocument.Parse(stdout);
            Assert.True(json.RootElement.TryGetProperty("isValid", out _));
            Assert.True(json.RootElement.TryGetProperty("canProceed", out _));
            Assert.True(json.RootElement.TryGetProperty("status", out _));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidateTransaction_InvalidJson_ReturnsError()
    {
        // Create a temp file with invalid JSON
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "{ invalid json }");

            // Act
            var (exitCode, stdout, _) = await RunCliAsync("validate-transaction", "--file", tempFile);

            // Assert
            Assert.Equal(1, exitCode);

            var json = JsonDocument.Parse(stdout);
            Assert.True(json.RootElement.TryGetProperty("error", out _));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Help and Version Tests

    [Fact]
    public async Task Help_DisplaysCommandList()
    {
        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync("--help");

        // Assert - CommandLineParser returns 1 for --help with error text in stdout
        var output = stdout + stderr;
        Assert.Contains("validate-transaction", output);
        Assert.Contains("lookup-customer", output);
        Assert.Contains("lookup-licence", output);
        Assert.Contains("generate-report", output);
    }

    [Fact]
    public async Task Version_DisplaysVersionInfo()
    {
        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync("--version");

        // Assert
        var output = stdout + stderr;
        Assert.Contains("RE2.ComplianceCli", output);
    }

    #endregion

    #region Helper Methods

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_cliProjectPath}\" --no-build -- {string.Join(" ", args)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdoutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with timeout
        var completed = await Task.Run(() => process.WaitForExit(60000));
        if (!completed)
        {
            process.Kill();
            throw new TimeoutException("CLI process timed out");
        }

        return (process.ExitCode, stdoutBuilder.ToString().Trim(), stderrBuilder.ToString().Trim());
    }

    #endregion
}
