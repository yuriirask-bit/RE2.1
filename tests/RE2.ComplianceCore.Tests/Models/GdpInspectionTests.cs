using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpInspection and GdpInspectionFinding domain models.
/// T213: TDD tests for GdpInspection per User Story 9 (FR-040).
/// </summary>
public class GdpInspectionTests
{
    #region GdpInspectionType Enum Tests

    [Theory]
    [InlineData(GdpInspectionType.RegulatoryAuthority, "RegulatoryAuthority")]
    [InlineData(GdpInspectionType.Internal, "Internal")]
    [InlineData(GdpInspectionType.SelfInspection, "SelfInspection")]
    public void GdpInspectionType_AllTypesAreDefined(GdpInspectionType type, string expectedName)
    {
        Assert.Equal(expectedName, type.ToString());
    }

    [Fact]
    public void GdpInspectionType_HasExpectedValues()
    {
        var values = Enum.GetValues<GdpInspectionType>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region FindingClassification Enum Tests

    [Theory]
    [InlineData(FindingClassification.Critical, "Critical")]
    [InlineData(FindingClassification.Major, "Major")]
    [InlineData(FindingClassification.Other, "Other")]
    public void FindingClassification_AllClassificationsAreDefined(FindingClassification classification, string expectedName)
    {
        Assert.Equal(expectedName, classification.ToString());
    }

    [Fact]
    public void FindingClassification_HasExpectedValues()
    {
        var values = Enum.GetValues<FindingClassification>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region GdpInspection Property Tests

    [Fact]
    public void GdpInspection_DefaultValues_AreCorrect()
    {
        var inspection = new GdpInspection();

        Assert.Equal(Guid.Empty, inspection.InspectionId);
        Assert.Equal(string.Empty, inspection.InspectorName);
        Assert.Equal(default, inspection.InspectionDate);
        Assert.Equal(default, inspection.InspectionType);
        Assert.Equal(Guid.Empty, inspection.SiteId);
        Assert.Null(inspection.WdaLicenceId);
        Assert.Null(inspection.FindingsSummary);
        Assert.Null(inspection.ReportReferenceUrl);
    }

    [Fact]
    public void GdpInspection_SetProperties_ReturnsCorrectValues()
    {
        var id = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var wdaId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 15);

        var inspection = new GdpInspection
        {
            InspectionId = id,
            InspectionDate = date,
            InspectorName = "IGJ Netherlands",
            InspectionType = GdpInspectionType.RegulatoryAuthority,
            SiteId = siteId,
            WdaLicenceId = wdaId,
            FindingsSummary = "Two findings identified",
            ReportReferenceUrl = "https://reports.example.com/IGJ-2026-001.pdf"
        };

        Assert.Equal(id, inspection.InspectionId);
        Assert.Equal(date, inspection.InspectionDate);
        Assert.Equal("IGJ Netherlands", inspection.InspectorName);
        Assert.Equal(GdpInspectionType.RegulatoryAuthority, inspection.InspectionType);
        Assert.Equal(siteId, inspection.SiteId);
        Assert.Equal(wdaId, inspection.WdaLicenceId);
        Assert.Equal("Two findings identified", inspection.FindingsSummary);
        Assert.Equal("https://reports.example.com/IGJ-2026-001.pdf", inspection.ReportReferenceUrl);
    }

    #endregion

    #region GdpInspection Validation Tests

    [Fact]
    public void GdpInspection_Validate_ValidInspection_ReturnsSuccess()
    {
        var inspection = CreateValidInspection();

        var result = inspection.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void GdpInspection_Validate_EmptyInspectorName_ReturnsFailure()
    {
        var inspection = CreateValidInspection();
        inspection.InspectorName = string.Empty;

        var result = inspection.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("InspectorName"));
    }

    [Fact]
    public void GdpInspection_Validate_EmptySiteId_ReturnsFailure()
    {
        var inspection = CreateValidInspection();
        inspection.SiteId = Guid.Empty;

        var result = inspection.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("SiteId"));
    }

    [Fact]
    public void GdpInspection_Validate_FutureDate_ReturnsFailure()
    {
        var inspection = CreateValidInspection();
        inspection.InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var result = inspection.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("future"));
    }

    [Fact]
    public void GdpInspection_Validate_NullWdaLicenceId_IsValid()
    {
        var inspection = CreateValidInspection();
        inspection.WdaLicenceId = null;

        var result = inspection.Validate();

        Assert.True(result.IsValid);
    }

    #endregion

    #region GdpInspectionFinding Property Tests

    [Fact]
    public void GdpInspectionFinding_DefaultValues_AreCorrect()
    {
        var finding = new GdpInspectionFinding();

        Assert.Equal(Guid.Empty, finding.FindingId);
        Assert.Equal(Guid.Empty, finding.InspectionId);
        Assert.Equal(string.Empty, finding.FindingDescription);
        Assert.Equal(default, finding.Classification);
        Assert.Null(finding.FindingNumber);
    }

    [Fact]
    public void GdpInspectionFinding_SetProperties_ReturnsCorrectValues()
    {
        var findingId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        var finding = new GdpInspectionFinding
        {
            FindingId = findingId,
            InspectionId = inspectionId,
            FindingDescription = "Temperature mapping not performed in last 12 months",
            Classification = FindingClassification.Major,
            FindingNumber = "IGJ-2026-001-F01"
        };

        Assert.Equal(findingId, finding.FindingId);
        Assert.Equal(inspectionId, finding.InspectionId);
        Assert.Equal("Temperature mapping not performed in last 12 months", finding.FindingDescription);
        Assert.Equal(FindingClassification.Major, finding.Classification);
        Assert.Equal("IGJ-2026-001-F01", finding.FindingNumber);
    }

    #endregion

    #region GdpInspectionFinding Validation Tests

    [Fact]
    public void GdpInspectionFinding_Validate_ValidFinding_ReturnsSuccess()
    {
        var finding = CreateValidFinding();

        var result = finding.Validate();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GdpInspectionFinding_Validate_EmptyDescription_ReturnsFailure()
    {
        var finding = CreateValidFinding();
        finding.FindingDescription = string.Empty;

        var result = finding.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("FindingDescription"));
    }

    [Fact]
    public void GdpInspectionFinding_Validate_EmptyInspectionId_ReturnsFailure()
    {
        var finding = CreateValidFinding();
        finding.InspectionId = Guid.Empty;

        var result = finding.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("InspectionId"));
    }

    [Fact]
    public void GdpInspectionFinding_IsCritical_ReturnsTrueForCritical()
    {
        var finding = CreateValidFinding();
        finding.Classification = FindingClassification.Critical;

        Assert.True(finding.IsCritical());
    }

    [Fact]
    public void GdpInspectionFinding_IsCritical_ReturnsFalseForMajor()
    {
        var finding = CreateValidFinding();
        finding.Classification = FindingClassification.Major;

        Assert.False(finding.IsCritical());
    }

    #endregion

    #region Helpers

    private static GdpInspection CreateValidInspection() => new()
    {
        InspectionId = Guid.NewGuid(),
        InspectionDate = new DateOnly(2026, 1, 15),
        InspectorName = "IGJ Netherlands",
        InspectionType = GdpInspectionType.RegulatoryAuthority,
        SiteId = Guid.NewGuid(),
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };

    private static GdpInspectionFinding CreateValidFinding() => new()
    {
        FindingId = Guid.NewGuid(),
        InspectionId = Guid.NewGuid(),
        FindingDescription = "Temperature mapping not performed",
        Classification = FindingClassification.Major
    };

    #endregion
}
