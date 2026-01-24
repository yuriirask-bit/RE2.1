using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;
using System.Security.Claims;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// Unit tests for TransactionsController.
/// T149b: API controller tests per FR-018 through FR-024.
/// </summary>
public class TransactionsControllerTests
{
    private readonly Mock<ITransactionComplianceService> _mockComplianceService;
    private readonly Mock<ILogger<TransactionsController>> _mockLogger;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _mockComplianceService = new Mock<ITransactionComplianceService>();
        _mockLogger = new Mock<ILogger<TransactionsController>>();

        _controller = new TransactionsController(
            _mockComplianceService.Object,
            _mockLogger.Object);

        // Setup default HttpContext with user
        var claims = new[] { new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region POST /api/v1/transactions/validate Tests

    [Fact]
    public async Task ValidateTransaction_ReturnsOk_WhenValidationPasses()
    {
        // Arrange
        var request = CreateValidationRequest();
        var transaction = CreateTransaction();
        var validationResult = TransactionValidationResult.Success(
            transaction,
            Enumerable.Empty<TransactionLicenceUsage>(),
            50);

        _mockComplianceService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateTransaction(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionValidationResultDto>().Subject;
        response.IsValid.Should().BeTrue();
        response.CanProceed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTransaction_ReturnsOk_WhenValidationFailsWithOverride()
    {
        // Arrange
        var request = CreateValidationRequest();
        var transaction = CreateTransaction();
        transaction.ValidationStatus = ValidationStatus.Failed;
        transaction.RequiresOverride = true;

        var violations = new[]
        {
            new ValidationViolation
            {
                ErrorCode = ErrorCodes.LICENCE_MISSING,
                Message = "No valid licence found",
                Severity = ViolationSeverity.Critical,
                CanOverride = true
            }
        };

        var validationResult = TransactionValidationResult.Failure(
            transaction,
            violations,
            Enumerable.Empty<TransactionLicenceUsage>(),
            100);

        _mockComplianceService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateTransaction(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionValidationResultDto>().Subject;
        response.IsValid.Should().BeFalse();
        response.CanOverride.Should().BeTrue();
        response.Violations.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateTransaction_ReturnsBadRequest_WhenExceptionThrown()
    {
        // Arrange
        var request = CreateValidationRequest();

        _mockComplianceService
            .Setup(s => s.ValidateTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.ValidateTransaction(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.INTERNAL_ERROR);
    }

    #endregion

    #region GET /api/v1/transactions/{id} Tests

    [Fact]
    public async Task GetTransaction_ReturnsOk_WhenTransactionExists()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTransaction();
        transaction.Id = transactionId;

        _mockComplianceService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.GetTransaction(transactionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionResponseDto>().Subject;
        response.Id.Should().Be(transactionId);
    }

    [Fact]
    public async Task GetTransaction_ReturnsNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _mockComplianceService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _controller.GetTransaction(transactionId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/transactions/by-external/{externalId} Tests

    [Fact]
    public async Task GetTransactionByExternalId_ReturnsOk_WhenTransactionExists()
    {
        // Arrange
        var externalId = "ORD-2024-001";
        var transaction = CreateTransaction();
        transaction.ExternalId = externalId;

        _mockComplianceService
            .Setup(s => s.GetTransactionByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.GetTransactionByExternalId(externalId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionResponseDto>().Subject;
        response.ExternalId.Should().Be(externalId);
    }

    [Fact]
    public async Task GetTransactionByExternalId_ReturnsNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        var externalId = "NONEXISTENT-001";

        _mockComplianceService
            .Setup(s => s.GetTransactionByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _controller.GetTransactionByExternalId(externalId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/transactions Tests

    [Fact]
    public async Task GetTransactions_ReturnsAllTransactions_WhenNoFilters()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(),
            CreateTransaction()
        };

        _mockComplianceService
            .Setup(s => s.GetTransactionsAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetTransactions();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<TransactionResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactions_FiltersbyStatus_WhenStatusProvided()
    {
        // Arrange
        var transactions = new[] { CreateTransaction() };

        _mockComplianceService
            .Setup(s => s.GetTransactionsAsync(ValidationStatus.Passed, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetTransactions(status: "Passed");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockComplianceService.Verify(s => s.GetTransactionsAsync(
            ValidationStatus.Passed, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_ReturnsEmpty_WhenNoTransactionsFound()
    {
        // Arrange
        _mockComplianceService
            .Setup(s => s.GetTransactionsAsync(It.IsAny<ValidationStatus?>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Transaction>());

        // Act
        var result = await _controller.GetTransactions();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<TransactionResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/transactions/pending Tests

    [Fact]
    public async Task GetPendingOverrides_ReturnsTransactions()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(),
            CreateTransaction()
        };
        foreach (var t in transactions)
        {
            t.RequiresOverride = true;
            t.OverrideStatus = OverrideStatus.Pending;
        }

        _mockComplianceService
            .Setup(s => s.GetPendingOverridesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetPendingOverrides();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<TransactionResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    #endregion

    #region GET /api/v1/transactions/pending/count Tests

    [Fact]
    public async Task GetPendingOverrideCount_ReturnsCount()
    {
        // Arrange
        _mockComplianceService
            .Setup(s => s.GetPendingOverrideCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.GetPendingOverrideCount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PendingOverrideCountDto>().Subject;
        response.Count.Should().Be(5);
    }

    #endregion

    #region POST /api/v1/transactions/{id}/approve Tests

    [Fact]
    public async Task ApproveOverride_ReturnsOk_WhenApprovalSucceeds()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideApprovalRequestDto { Justification = "Business justification" };
        var transaction = CreateTransaction();
        transaction.Id = transactionId;
        transaction.ValidationStatus = ValidationStatus.ApprovedWithOverride;
        transaction.OverrideStatus = OverrideStatus.Approved;

        _mockComplianceService
            .Setup(s => s.ApproveOverrideAsync(transactionId, It.IsAny<string>(), request.Justification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockComplianceService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.ApproveOverride(transactionId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionResponseDto>().Subject;
        response.Id.Should().Be(transactionId);
    }

    [Fact]
    public async Task ApproveOverride_ReturnsBadRequest_WhenJustificationEmpty()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideApprovalRequestDto { Justification = "" };

        // Act
        var result = await _controller.ApproveOverride(transactionId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task ApproveOverride_ReturnsNotFound_WhenTransactionNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideApprovalRequestDto { Justification = "Justification" };

        _mockComplianceService
            .Setup(s => s.ApproveOverrideAsync(transactionId, It.IsAny<string>(), request.Justification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(ErrorCodes.TRANSACTION_NOT_FOUND, "Transaction not found"));

        // Act
        var result = await _controller.ApproveOverride(transactionId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    [Fact]
    public async Task ApproveOverride_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideApprovalRequestDto { Justification = "Justification" };

        _mockComplianceService
            .Setup(s => s.ApproveOverrideAsync(transactionId, It.IsAny<string>(), request.Justification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "Override not required"));

        // Act
        var result = await _controller.ApproveOverride(transactionId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region POST /api/v1/transactions/{id}/reject Tests

    [Fact]
    public async Task RejectOverride_ReturnsOk_WhenRejectionSucceeds()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideRejectionRequestDto { Reason = "Insufficient justification" };
        var transaction = CreateTransaction();
        transaction.Id = transactionId;
        transaction.ValidationStatus = ValidationStatus.RejectedOverride;
        transaction.OverrideStatus = OverrideStatus.Rejected;

        _mockComplianceService
            .Setup(s => s.RejectOverrideAsync(transactionId, It.IsAny<string>(), request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockComplianceService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.RejectOverride(transactionId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionResponseDto>().Subject;
        response.Id.Should().Be(transactionId);
    }

    [Fact]
    public async Task RejectOverride_ReturnsBadRequest_WhenReasonEmpty()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideRejectionRequestDto { Reason = "  " };

        // Act
        var result = await _controller.RejectOverride(transactionId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task RejectOverride_ReturnsNotFound_WhenTransactionNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new OverrideRejectionRequestDto { Reason = "Reason" };

        _mockComplianceService
            .Setup(s => s.RejectOverrideAsync(transactionId, It.IsAny<string>(), request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(ErrorCodes.TRANSACTION_NOT_FOUND, "Transaction not found"));

        // Act
        var result = await _controller.RejectOverride(transactionId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    #endregion

    #region Helper Methods

    private static TransactionValidationRequestDto CreateValidationRequest()
    {
        return new TransactionValidationRequestDto
        {
            ExternalId = "ORD-2024-001",
            TransactionType = "Order",
            Direction = "Internal",
            CustomerId = Guid.NewGuid(),
            OriginCountry = "NL",
            TransactionDate = DateTime.UtcNow,
            Lines = new List<TransactionLineDto>
            {
                new TransactionLineDto
                {
                    SubstanceId = Guid.NewGuid(),
                    SubstanceCode = "MORPH-001",
                    Quantity = 100,
                    BaseUnitQuantity = 100,
                    BaseUnit = "g"
                }
            }
        };
    }

    private static Transaction CreateTransaction()
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            ExternalId = "ORD-2024-001",
            TransactionType = TransactionType.Order,
            Direction = TransactionDirection.Internal,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Pharmacy",
            OriginCountry = "NL",
            TransactionDate = DateTime.UtcNow,
            TotalQuantity = 100,
            Status = "Validated",
            ValidationStatus = ValidationStatus.Passed,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    #endregion
}
