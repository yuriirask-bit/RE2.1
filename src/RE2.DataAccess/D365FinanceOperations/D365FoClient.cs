using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Azure.Core;
using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Exceptions;
using RE2.ComplianceCore.Interfaces;

namespace RE2.DataAccess.D365FinanceOperations;

/// <summary>
/// Implementation of ID365FoClient using HttpClient with OData v4.
/// Per research.md section 2: Uses OAuth2 Client Credentials flow with Managed Identity.
/// Includes resilience handling via standard resilience handler (configured separately).
/// </summary>
public class D365FoClient : ID365FoClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenCredential _credential;
    private readonly string _resource;
    private readonly ILogger<D365FoClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of D365FoClient.
    /// </summary>
    /// <param name="httpClient">Pre-configured HttpClient with base address and resilience handler.</param>
    /// <param name="resource">D365 F&O resource URL for token acquisition.</param>
    /// <param name="credential">Token credential for OAuth2 authentication (managed identity or client credentials).</param>
    /// <param name="logger">Logger instance.</param>
    public D365FoClient(
        HttpClient httpClient,
        string resource,
        TokenCredential credential,
        ILogger<D365FoClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource URL is required", nameof(resource));
        }

        _resource = resource;

        // Configure JSON serialization options for OData
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("D365FoClient initialized with resource: {Resource}", resource);
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(
        string entitySetName,
        string? query = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var url = string.IsNullOrWhiteSpace(query)
            ? $"{entitySetName}?cross-company=true"
            : $"{entitySetName}?cross-company=true&{query}";

        try
        {
            _logger.LogDebug("GET request to D365 F&O: {Url}", url);

            await EnsureAuthTokenAsync(cancellationToken);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            await EnsureSuccessOrThrowWithDetailsAsync(response, url, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);

            _logger.LogDebug("GET request successful for {EntitySetName}", entitySetName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GET request to {Url}", url);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetByKeyAsync<T>(
        string entitySetName,
        string key,
        CancellationToken cancellationToken = default) where T : class
    {
        var url = $"{entitySetName}({key})?cross-company=true";

        try
        {
            _logger.LogDebug("GET by key request to D365 F&O: {Url}", url);

            await EnsureAuthTokenAsync(cancellationToken);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            await EnsureSuccessOrThrowWithDetailsAsync(response, url, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GET by key request to {Url}", url);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> CreateAsync<T>(
        string entitySetName,
        T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("POST request to D365 F&O: {EntitySetName}", entitySetName);

            await EnsureAuthTokenAsync(cancellationToken);

            var response = await _httpClient.PostAsJsonAsync(
                entitySetName,
                entity,
                _jsonOptions,
                cancellationToken);

            await EnsureSuccessOrThrowWithDetailsAsync(response, entitySetName, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Created entity in {EntitySetName}", entitySetName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity in {EntitySetName}", entitySetName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync<T>(
        string entitySetName,
        string key,
        T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        var url = $"{entitySetName}({key})";

        try
        {
            _logger.LogDebug("PATCH request to D365 F&O: {Url}", url);

            await EnsureAuthTokenAsync(cancellationToken);

            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrowWithDetailsAsync(response, url, cancellationToken);

            _logger.LogInformation("Updated entity in {EntitySetName} with key {Key}", entitySetName, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity in {EntitySetName} with key {Key}", entitySetName, key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateWithConcurrencyAsync<T>(
        string entitySetName,
        string key,
        T entity,
        string etag,
        CancellationToken cancellationToken = default) where T : class
    {
        var url = $"{entitySetName}({key})";

        try
        {
            _logger.LogDebug("PATCH request with ETag to D365 F&O: {Url} (ETag: {ETag})", url, etag);

            await EnsureAuthTokenAsync(cancellationToken);

            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };

            // Add If-Match header with ETag for optimistic concurrency
            request.Headers.IfMatch.Add(new EntityTagHeaderValue(etag, isWeak: false));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Handle PreconditionFailed (412) as concurrency conflict
            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                _logger.LogWarning("Concurrency conflict detected for {EntitySetName} with key {Key}. " +
                                   "Local ETag: {LocalETag}",
                    entitySetName, key, etag);

                // Try to extract entity ID from key
                Guid entityId = Guid.Empty;
                if (Guid.TryParse(key.Trim('\'', '"'), out var parsedId))
                {
                    entityId = parsedId;
                }

                throw new ConcurrencyException(
                    entitySetName,
                    entityId,
                    etag,
                    response.Headers.ETag?.Tag,
                    $"The {entitySetName} with key {key} has been modified by another user. Please refresh and try again.");
            }

            await EnsureSuccessOrThrowWithDetailsAsync(response, url, cancellationToken);

            _logger.LogInformation("Updated entity in {EntitySetName} with key {Key} with concurrency check", entitySetName, key);
        }
        catch (ConcurrencyException)
        {
            throw; // Re-throw concurrency exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity in {EntitySetName} with key {Key}", entitySetName, key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string entitySetName,
        string key,
        CancellationToken cancellationToken = default)
    {
        var url = $"{entitySetName}({key})";

        try
        {
            _logger.LogDebug("DELETE request to D365 F&O: {Url}", url);

            await EnsureAuthTokenAsync(cancellationToken);

            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            await EnsureSuccessOrThrowWithDetailsAsync(response, url, cancellationToken);

            _logger.LogInformation("Deleted entity from {EntitySetName} with key {Key}", entitySetName, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity from {EntitySetName} with key {Key}", entitySetName, key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TResponse?> ExecuteActionAsync<TRequest, TResponse>(
        string actionOrFunctionName,
        TRequest? request = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            _logger.LogDebug("Executing action/function: {ActionName}", actionOrFunctionName);

            await EnsureAuthTokenAsync(cancellationToken);

            HttpResponseMessage response;

            if (request != null)
            {
                response = await _httpClient.PostAsJsonAsync(
                    actionOrFunctionName,
                    request,
                    _jsonOptions,
                    cancellationToken);
            }
            else
            {
                response = await _httpClient.PostAsync(
                    actionOrFunctionName,
                    null,
                    cancellationToken);
            }

            await EnsureSuccessOrThrowWithDetailsAsync(response, actionOrFunctionName, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

            _logger.LogInformation("Executed action/function: {ActionName}", actionOrFunctionName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action/function: {ActionName}", actionOrFunctionName);
            throw;
        }
    }

    /// <summary>
    /// Reads the response body and throws HttpRequestException with the actual D365 F&O error
    /// message if the response indicates failure. Unlike EnsureSuccessStatusCode(), this
    /// preserves the server's error details instead of discarding the response body.
    /// </summary>
    private async Task EnsureSuccessOrThrowWithDetailsAsync(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "(none)";
        var contentLength = response.Content.Headers.ContentLength;
        var wwwAuth = response.Headers.WwwAuthenticate.ToString();

        _logger.LogError(
            "D365 F&O returned {StatusCode} for {Url}. " +
            "Content-Type: {ContentType}, Content-Length: {ContentLength}, " +
            "WWW-Authenticate: {WwwAuth}, Response body: [{Body}]",
            (int)response.StatusCode, url, contentType, contentLength, wwwAuth, body);

        var detail = !string.IsNullOrEmpty(body) ? body
            : !string.IsNullOrEmpty(wwwAuth) ? $"WWW-Authenticate: {wwwAuth}"
            : "(empty response)";

        throw new HttpRequestException(
            $"D365 F&O returned {(int)response.StatusCode} ({response.ReasonPhrase}) for {url}: {detail}",
            inner: null,
            statusCode: response.StatusCode);
    }

    /// <summary>
    /// Ensures the HttpClient has a valid authentication token.
    /// Per research.md section 2: Uses Managed Identity to acquire OAuth2 token.
    /// </summary>
    private async Task EnsureAuthTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _credential.GetTokenAsync(
                new TokenRequestContext(new[] { $"{_resource}/.default" }),
                cancellationToken);

            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire access token for D365 F&O");
            throw;
        }
    }
}
