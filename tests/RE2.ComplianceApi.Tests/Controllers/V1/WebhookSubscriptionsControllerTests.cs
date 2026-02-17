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

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// T149m: Integration tests for webhook subscription endpoints.
/// Tests WebhookSubscriptionsController with mocked dependencies.
/// </summary>
public class WebhookSubscriptionsControllerTests
{
    private readonly Mock<IWebhookSubscriptionRepository> _mockSubscriptionRepo;
    private readonly Mock<IIntegrationSystemRepository> _mockIntegrationSystemRepo;
    private readonly Mock<ILogger<WebhookSubscriptionsController>> _mockLogger;
    private readonly WebhookSubscriptionsController _controller;

    public WebhookSubscriptionsControllerTests()
    {
        _mockSubscriptionRepo = new Mock<IWebhookSubscriptionRepository>();
        _mockIntegrationSystemRepo = new Mock<IIntegrationSystemRepository>();
        _mockLogger = new Mock<ILogger<WebhookSubscriptionsController>>();

        _controller = new WebhookSubscriptionsController(
            _mockSubscriptionRepo.Object,
            _mockIntegrationSystemRepo.Object,
            _mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/webhooksubscriptions Tests

    [Fact]
    public async Task GetWebhookSubscriptions_ReturnsOk_WithAllSubscriptions()
    {
        // Arrange
        var subscriptions = new List<WebhookSubscription>
        {
            CreateTestSubscription(),
            CreateTestSubscription()
        };

        _mockSubscriptionRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _controller.GetWebhookSubscriptions();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebhookSubscriptionResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWebhookSubscriptions_FiltersBy_IntegrationSystemId()
    {
        // Arrange
        var integrationSystemId = Guid.NewGuid();
        var subscriptions = new List<WebhookSubscription>
        {
            CreateTestSubscription(integrationSystemId)
        };

        _mockSubscriptionRepo
            .Setup(r => r.GetByIntegrationSystemIdAsync(integrationSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _controller.GetWebhookSubscriptions(integrationSystemId: integrationSystemId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebhookSubscriptionResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWebhookSubscriptions_FiltersBy_ActiveOnly()
    {
        // Arrange
        var activeSubscription = CreateTestSubscription();
        activeSubscription.IsActive = true;

        _mockSubscriptionRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { activeSubscription });

        // Act
        var result = await _controller.GetWebhookSubscriptions(activeOnly: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebhookSubscriptionResponseDto>>().Subject;
        response.Should().OnlyContain(s => s.IsActive);
    }

    #endregion

    #region GET /api/v1/webhooksubscriptions/{id} Tests

    [Fact]
    public async Task GetWebhookSubscription_ReturnsOk_WhenSubscriptionExists()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        _mockSubscriptionRepo
            .Setup(r => r.GetByIdAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.GetWebhookSubscription(subscription.SubscriptionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<WebhookSubscriptionResponseDto>().Subject;
        response.SubscriptionId.Should().Be(subscription.SubscriptionId);
    }

    [Fact]
    public async Task GetWebhookSubscription_ReturnsNotFound_WhenSubscriptionDoesNotExist()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepo
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        // Act
        var result = await _controller.GetWebhookSubscription(subscriptionId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/webhooksubscriptions Tests

    [Fact]
    public async Task CreateWebhookSubscription_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var integrationSystemId = Guid.NewGuid();
        var integrationSystem = new IntegrationSystem
        {
            IntegrationSystemId = integrationSystemId,
            SystemName = "Test System",
            SystemType = IntegrationSystemType.ERP,
            ApiKeyHash = "hashedkey",
            IsActive = true
        };

        var request = new CreateWebhookSubscriptionRequestDto
        {
            IntegrationSystemId = integrationSystemId,
            EventTypes = new[] { "OrderApproved", "OrderRejected" },
            CallbackUrl = "https://example.com/webhook",
            SecretKey = "this-is-a-secret-key-that-is-at-least-32-characters-long"
        };

        _mockIntegrationSystemRepo
            .Setup(r => r.GetByIdAsync(integrationSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrationSystem);

        _mockSubscriptionRepo
            .Setup(r => r.ExistsByCallbackUrlAsync(integrationSystemId, request.CallbackUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockSubscriptionRepo
            .Setup(r => r.CreateAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription s, CancellationToken _) =>
            {
                s.SubscriptionId = Guid.NewGuid();
                return s;
            });

        // Act
        var result = await _controller.CreateWebhookSubscription(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<WebhookSubscriptionResponseDto>().Subject;
        response.CallbackUrl.Should().Be(request.CallbackUrl);
        response.EventTypes.Should().Contain("OrderApproved");
        response.EventTypes.Should().Contain("OrderRejected");
    }

    [Fact]
    public async Task CreateWebhookSubscription_ReturnsBadRequest_WhenIntegrationSystemNotFound()
    {
        // Arrange
        var integrationSystemId = Guid.NewGuid();
        var request = new CreateWebhookSubscriptionRequestDto
        {
            IntegrationSystemId = integrationSystemId,
            EventTypes = new[] { "OrderApproved" },
            CallbackUrl = "https://example.com/webhook",
            SecretKey = "this-is-a-secret-key-that-is-at-least-32-characters-long"
        };

        _mockIntegrationSystemRepo
            .Setup(r => r.GetByIdAsync(integrationSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegrationSystem?)null);

        // Act
        var result = await _controller.CreateWebhookSubscription(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task CreateWebhookSubscription_ReturnsBadRequest_WhenSecretKeyTooShort()
    {
        // Arrange
        var integrationSystemId = Guid.NewGuid();
        var integrationSystem = new IntegrationSystem
        {
            IntegrationSystemId = integrationSystemId,
            SystemName = "Test System",
            SystemType = IntegrationSystemType.ERP,
            ApiKeyHash = "hashedkey",
            IsActive = true
        };

        var request = new CreateWebhookSubscriptionRequestDto
        {
            IntegrationSystemId = integrationSystemId,
            EventTypes = new[] { "OrderApproved" },
            CallbackUrl = "https://example.com/webhook",
            SecretKey = "too-short"
        };

        _mockIntegrationSystemRepo
            .Setup(r => r.GetByIdAsync(integrationSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrationSystem);

        // Act
        var result = await _controller.CreateWebhookSubscription(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Details.Should().Contain("32 characters");
    }

    [Fact]
    public async Task CreateWebhookSubscription_ReturnsBadRequest_WhenDuplicateCallbackUrl()
    {
        // Arrange
        var integrationSystemId = Guid.NewGuid();
        var integrationSystem = new IntegrationSystem
        {
            IntegrationSystemId = integrationSystemId,
            SystemName = "Test System",
            SystemType = IntegrationSystemType.ERP,
            ApiKeyHash = "hashedkey",
            IsActive = true
        };

        var request = new CreateWebhookSubscriptionRequestDto
        {
            IntegrationSystemId = integrationSystemId,
            EventTypes = new[] { "OrderApproved" },
            CallbackUrl = "https://example.com/webhook",
            SecretKey = "this-is-a-secret-key-that-is-at-least-32-characters-long"
        };

        _mockIntegrationSystemRepo
            .Setup(r => r.GetByIdAsync(integrationSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(integrationSystem);

        _mockSubscriptionRepo
            .Setup(r => r.ExistsByCallbackUrlAsync(integrationSystemId, request.CallbackUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateWebhookSubscription(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Message.Should().Contain("already exists");
    }

    #endregion

    #region DELETE /api/v1/webhooksubscriptions/{id} Tests

    [Fact]
    public async Task DeleteWebhookSubscription_ReturnsNoContent_WhenSubscriptionExists()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        _mockSubscriptionRepo
            .Setup(r => r.GetByIdAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepo
            .Setup(r => r.DeleteAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteWebhookSubscription(subscription.SubscriptionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteWebhookSubscription_ReturnsNotFound_WhenSubscriptionDoesNotExist()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepo
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        // Act
        var result = await _controller.DeleteWebhookSubscription(subscriptionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region POST /api/v1/webhooksubscriptions/{id}/reactivate Tests

    [Fact]
    public async Task ReactivateWebhookSubscription_ReturnsOk_WithReactivatedSubscription()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.IsActive = false;
        subscription.FailedAttempts = 10;

        _mockSubscriptionRepo
            .Setup(r => r.GetByIdAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepo
            .Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription s, CancellationToken _) => s);

        // Act
        var result = await _controller.ReactivateWebhookSubscription(subscription.SubscriptionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<WebhookSubscriptionResponseDto>().Subject;
        response.IsActive.Should().BeTrue();
        response.FailedAttempts.Should().Be(0);
    }

    #endregion

    #region GET /api/v1/webhooksubscriptions/event-types Tests

    [Fact]
    public void GetEventTypes_ReturnsOk_WithAllEventTypes()
    {
        // Act
        var result = _controller.GetEventTypes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebhookEventTypeDto>>().Subject;
        response.Should().Contain(e => e.Name == "ComplianceStatusChanged");
        response.Should().Contain(e => e.Name == "OrderApproved");
        response.Should().Contain(e => e.Name == "OrderRejected");
        response.Should().Contain(e => e.Name == "LicenceExpiring");
        response.Should().Contain(e => e.Name == "OverrideApproved");
    }

    #endregion

    #region Helper Methods

    private static WebhookSubscription CreateTestSubscription(Guid? integrationSystemId = null)
    {
        return new WebhookSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            IntegrationSystemId = integrationSystemId ?? Guid.NewGuid(),
            EventTypes = WebhookEventType.OrderApproved | WebhookEventType.OrderRejected,
            CallbackUrl = "https://example.com/webhook",
            SecretKey = "this-is-a-secret-key-that-is-at-least-32-characters-long",
            IsActive = true,
            Description = "Test subscription",
            FailedAttempts = 0,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    #endregion
}
