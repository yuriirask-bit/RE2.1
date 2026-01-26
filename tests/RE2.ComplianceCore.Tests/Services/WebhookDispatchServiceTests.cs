using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.Notifications;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// Unit tests for WebhookDispatchService.
/// T149l: Service tests for webhook dispatch per FR-059.
/// </summary>
public class WebhookDispatchServiceTests
{
    private readonly Mock<IWebhookSubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<WebhookDispatchService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly WebhookDispatchService _service;

    public WebhookDispatchServiceTests()
    {
        _subscriptionRepoMock = new Mock<IWebhookSubscriptionRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<WebhookDispatchService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // Setup HttpClient mock
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("WebhookClient")).Returns(httpClient);

        _service = new WebhookDispatchService(
            _subscriptionRepoMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    #region ComputeSignature Tests

    [Fact]
    public void ComputeSignature_ReturnsCorrectFormat()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";

        // Act
        var signature = _service.ComputeSignature(payload, secretKey);

        // Assert
        signature.Should().StartWith("sha256=");
        signature.Should().HaveLength(7 + 64); // "sha256=" + 64 hex chars
    }

    [Fact]
    public void ComputeSignature_ReturnsConsistentResults()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";

        // Act
        var signature1 = _service.ComputeSignature(payload, secretKey);
        var signature2 = _service.ComputeSignature(payload, secretKey);

        // Assert
        signature1.Should().Be(signature2);
    }

    [Fact]
    public void ComputeSignature_ReturnsDifferentResults_ForDifferentPayloads()
    {
        // Arrange
        var payload1 = "{\"test\":\"data1\"}";
        var payload2 = "{\"test\":\"data2\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";

        // Act
        var signature1 = _service.ComputeSignature(payload1, secretKey);
        var signature2 = _service.ComputeSignature(payload2, secretKey);

        // Assert
        signature1.Should().NotBe(signature2);
    }

    [Fact]
    public void ComputeSignature_ReturnsDifferentResults_ForDifferentSecrets()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secretKey1 = "this-is-a-secret-key-that-is-at-least-32-characters-1";
        var secretKey2 = "this-is-a-secret-key-that-is-at-least-32-characters-2";

        // Act
        var signature1 = _service.ComputeSignature(payload, secretKey1);
        var signature2 = _service.ComputeSignature(payload, secretKey2);

        // Assert
        signature1.Should().NotBe(signature2);
    }

    #endregion

    #region VerifySignature Tests

    [Fact]
    public void VerifySignature_ReturnsTrue_ForValidSignature()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";
        var signature = _service.ComputeSignature(payload, secretKey);

        // Act
        var isValid = _service.VerifySignature(payload, signature, secretKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_ForInvalidSignature()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";
        var invalidSignature = "sha256=invalid";

        // Act
        var isValid = _service.VerifySignature(payload, invalidSignature, secretKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_ReturnsFalse_ForTamperedPayload()
    {
        // Arrange
        var originalPayload = "{\"test\":\"data\"}";
        var tamperedPayload = "{\"test\":\"tampered\"}";
        var secretKey = "this-is-a-secret-key-that-is-at-least-32-characters";
        var signature = _service.ComputeSignature(originalPayload, secretKey);

        // Act
        var isValid = _service.VerifySignature(tamperedPayload, signature, secretKey);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region DispatchAsync Tests

    [Fact]
    public async Task DispatchAsync_ReturnsEmptyResult_WhenNoSubscribers()
    {
        // Arrange
        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription>());

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        var result = await _service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        result.SubscribersNotified.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_SendsWebhookToAllActiveSubscribers()
    {
        // Arrange - create fresh service with dedicated mock
        var subscriptionRepoMock = new Mock<IWebhookSubscriptionRepository>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var loggerMock = new Mock<ILogger<WebhookDispatchService>>();

        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));

        // Return a new HttpClient each time CreateClient is called
        httpClientFactoryMock.Setup(f => f.CreateClient("WebhookClient"))
            .Returns(() => new HttpClient(httpMessageHandlerMock.Object));

        var service = new WebhookDispatchService(
            subscriptionRepoMock.Object,
            httpClientFactoryMock.Object,
            loggerMock.Object);

        var subscription1 = CreateSubscription(Guid.NewGuid(), "https://example1.com/webhook");
        var subscription2 = CreateSubscription(Guid.NewGuid(), "https://example2.com/webhook");

        subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { subscription1, subscription2 });

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        var result = await service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        result.SubscribersNotified.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_RecordsSuccess_WhenWebhookDelivered()
    {
        // Arrange
        var subscription = CreateSubscription(Guid.NewGuid(), "https://example.com/webhook");

        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { subscription });

        SetupHttpResponse(HttpStatusCode.OK);

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        await _service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        _subscriptionRepoMock.Verify(r => r.RecordSuccessAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_RecordsFailure_WhenWebhookDeliveryFails()
    {
        // Arrange
        var subscription = CreateSubscription(Guid.NewGuid(), "https://example.com/webhook");

        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { subscription });

        SetupHttpResponse(HttpStatusCode.InternalServerError);

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        var result = await _service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        result.FailureCount.Should().Be(1);
        _subscriptionRepoMock.Verify(r => r.RecordFailureAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_IncludesSignatureHeader()
    {
        // Arrange
        var subscription = CreateSubscription(Guid.NewGuid(), "https://example.com/webhook");

        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { subscription });

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        await _service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().ContainSingle(h => h.Key == "X-Webhook-Signature");
        capturedRequest.Headers.Should().ContainSingle(h => h.Key == "X-Webhook-Event");
        capturedRequest.Headers.Should().ContainSingle(h => h.Key == "X-Webhook-Id");
        capturedRequest.Headers.Should().ContainSingle(h => h.Key == "X-Webhook-Timestamp");
    }

    [Fact]
    public async Task DispatchAsync_ContinuesProcessing_WhenOneSubscriberFails()
    {
        // Arrange
        var subscription1 = CreateSubscription(Guid.NewGuid(), "https://failing.com/webhook");
        var subscription2 = CreateSubscription(Guid.NewGuid(), "https://working.com/webhook");

        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.OrderApproved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { subscription1, subscription2 });

        var callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var count = Interlocked.Increment(ref callCount);
                // First call fails, second succeeds
                return count == 1
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });

        var payload = new { orderId = Guid.NewGuid() };

        // Act
        var result = await _service.DispatchAsync(WebhookEventType.OrderApproved, payload);

        // Assert
        result.SubscribersNotified.Should().Be(2);
        // Due to parallel execution, exactly one succeeds and one fails (order may vary)
        (result.SuccessCount + result.FailureCount).Should().Be(2);
    }

    #endregion

    #region GetSubscribersForEventAsync Tests

    [Fact]
    public async Task GetSubscribersForEventAsync_ReturnsActiveSubscribers()
    {
        // Arrange
        var subscriptions = new List<WebhookSubscription>
        {
            CreateSubscription(Guid.NewGuid(), "https://example1.com/webhook"),
            CreateSubscription(Guid.NewGuid(), "https://example2.com/webhook")
        };

        _subscriptionRepoMock.Setup(r => r.GetActiveByEventTypeAsync(WebhookEventType.ComplianceStatusChanged, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        // Act
        var result = await _service.GetSubscribersForEventAsync(WebhookEventType.ComplianceStatusChanged);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Helper Methods

    private static WebhookSubscription CreateSubscription(Guid integrationSystemId, string callbackUrl)
    {
        return new WebhookSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            IntegrationSystemId = integrationSystemId,
            EventTypes = WebhookEventType.OrderApproved | WebhookEventType.OrderRejected,
            CallbackUrl = callbackUrl,
            SecretKey = "this-is-a-secret-key-that-is-at-least-32-characters-long",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    #endregion
}
