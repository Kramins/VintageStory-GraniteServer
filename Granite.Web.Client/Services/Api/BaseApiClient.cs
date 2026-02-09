using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Base class for API clients with common functionality for error handling and response mapping.
/// </summary>
public abstract class BaseApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    protected readonly ILogger<BaseApiClient> Logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    protected BaseApiClient(IHttpClientFactory httpClientFactory, ILogger<BaseApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        Logger = logger;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    /// <summary>
    /// Gets an HttpClient instance from the factory.
    /// </summary>
    protected HttpClient GetHttpClient() => _httpClientFactory.CreateClient("GraniteApi");

    /// <summary>
    /// Makes a GET request and deserializes the response as JsonApiDocument{T}.
    /// </summary>
    protected async Task<JsonApiDocument<T>> GetAsync<T>(string url)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.GetAsync(url);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for GET {Url}", url);
            throw new ApiException($"Failed to get data from {url}", ex);
        }
    }

    /// <summary>
    /// Makes a POST request and deserializes the response as JsonApiDocument{T}.
    /// </summary>
    protected async Task<JsonApiDocument<T>> PostAsync<T>(string url, object? content = null)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = content == null
                ? await httpClient.PostAsync(url, null)
                : await httpClient.PostAsJsonAsync(url, content);

            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for POST {Url}", url);
            throw new ApiException($"Failed to post data to {url}", ex);
        }
    }

    /// <summary>
    /// Makes a PUT request and deserializes the response as JsonApiDocument{T}.
    /// </summary>
    protected async Task<JsonApiDocument<T>> PutAsync<T>(string url, object content)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PutAsJsonAsync(url, content);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for PUT {Url}", url);
            throw new ApiException($"Failed to update data at {url}", ex);
        }
    }

    /// <summary>
    /// Makes a PATCH request and deserializes the response as JsonApiDocument{T}.
    /// </summary>
    protected async Task<JsonApiDocument<T>> PatchAsync<T>(string url, object content)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PatchAsJsonAsync(url, content);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for PATCH {Url}", url);
            throw new ApiException($"Failed to patch data at {url}", ex);
        }
    }

    /// <summary>
    /// Makes a DELETE request.
    /// </summary>
    protected async Task DeleteAsync(string url)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponse(response);
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed for DELETE {Url}", url);
            throw new ApiException($"Failed to delete data at {url}", ex);
        }
    }

    /// <summary>
    /// Handles the HTTP response and deserializes it as JsonApiDocument{T}.
    /// </summary>
    private async Task<JsonApiDocument<T>> HandleResponse<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                Logger.LogInformation("API Response Content: {Content}", content);
                
                if (string.IsNullOrEmpty(content))
                {
                    return new JsonApiDocument<T> { Data = default };
                }

                // Deserialize as JsonApiDocument<T>
                var document = JsonSerializer.Deserialize<JsonApiDocument<T>>(content, _jsonSerializerOptions);
                
                Logger.LogInformation("Deserialized as JsonApiDocument - Document is null: {IsNull}", document == null);
                
                // Check if we got a valid JsonApiDocument with data
                if (document != null && document.Data != null)
                {
                    Logger.LogInformation("JsonApiDocument has data, returning it");
                    return document;
                }

                // Fallback: If we can't deserialize as JsonApiDocument or Data is null, try direct deserialization
                Logger.LogInformation("Attempting fallback deserialization...");
                var data = JsonSerializer.Deserialize<T>(content, _jsonSerializerOptions);
                Logger.LogInformation("Fallback deserialization - HasData: {HasData}", data is not null);
                return new JsonApiDocument<T> { Data = data };
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "Failed to deserialize JSON response");
                throw new ApiException("Failed to deserialize response", ex);
            }
        }

        await HandleErrorResponse(response);
        throw new ApiException("Unexpected error occurred");
    }

    /// <summary>
    /// Handles error responses from the API.
    /// </summary>
    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Logger.LogError("API error response: {StatusCode} {Content}", response.StatusCode, content);

        // Try to parse JSON error response
        try
        {
            if (!string.IsNullOrEmpty(content))
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var errorCode = root.TryGetProperty("code", out var codeElement)
                    ? codeElement.GetString()
                    : null;

                var errorMessage = root.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : null;

                throw new ApiException(
                    $"API returned {response.StatusCode}: {errorMessage ?? "Unknown error"}",
                    (int)response.StatusCode,
                    errorCode,
                    errorMessage
                );
            }
        }
        catch (JsonException)
        {
            // If we can't parse JSON, use the status code
        }

        throw new ApiException(
            $"API request failed with status code {response.StatusCode}",
            (int)response.StatusCode
        );
    }
}
