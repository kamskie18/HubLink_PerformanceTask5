using System.Text.Json;
using HubLink.Application.Configuration;
using HubLink.Application.Models;
using HubLink.Domain.Models;
using Microsoft.Extensions.Options;

namespace HubLink.Application.Services;

public class HubLinkApiClient : IHubLinkApiClient
{
    private readonly HttpClient _httpClient;
    private readonly HubLinkOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HubLinkApiClient(HttpClient httpClient, IOptions<HubLinkOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }
    }

    public async Task<ApiResponse<BranchShipmentsResponse>> GetShipmentsAsync(
        string? branchCode = null,
        bool useTestEndpoint = false,
        string? testMode = null,
        string? baseAddress = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveBase = string.IsNullOrWhiteSpace(baseAddress) ? _options.BaseAddress : baseAddress.Trim();
        if (!Uri.TryCreate(effectiveBase.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            return ApiResponse<BranchShipmentsResponse>.Failure(
                $"Invalid service base URL '{effectiveBase}'. Expected an HTTP or HTTPS address (e.g. http://127.0.0.1:5080).",
                400);
        }

        var targetBranch = string.IsNullOrWhiteSpace(branchCode) ? _options.BranchCode : branchCode.Trim();
        if (string.IsNullOrWhiteSpace(targetBranch))
        {
            return ApiResponse<BranchShipmentsResponse>.Failure("Branch code must not be empty.", 400);
        }

        string relativePath;
        if (useTestEndpoint)
        {
            var mode = string.IsNullOrWhiteSpace(testMode) ? (_options.TestMode ?? "normal") : testMode.Trim();
            relativePath = $"api/test/shipments?branchCode={Uri.EscapeDataString(targetBranch)}&mode={Uri.EscapeDataString(mode)}";
        }
        else
        {
            relativePath = $"api/shipments?branchCode={Uri.EscapeDataString(targetBranch)}";
        }

        var requestUri = new Uri(baseUri, relativePath);

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMsg = TryExtractErrorMessage(errorBody)
                    ?? $"Service returned HTTP {statusCode} ({response.ReasonPhrase}).";
                return ApiResponse<BranchShipmentsResponse>.Failure(errorMsg, statusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<BranchShipmentsResponse>.Failure(
                    "Service returned an empty response body instead of shipment JSON.",
                    statusCode);
            }

            BranchShipmentsResponse? result;
            try
            {
                result = JsonSerializer.Deserialize<BranchShipmentsResponse>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                return ApiResponse<BranchShipmentsResponse>.Failure(
                    $"Unable to interpret service response: malformed or incomplete JSON. ({ex.Message})",
                    statusCode);
            }

            if (result == null || string.IsNullOrWhiteSpace(result.BranchCode))
            {
                return ApiResponse<BranchShipmentsResponse>.Failure(
                    "Service response could not be interpreted as valid branch shipment data.",
                    statusCode);
            }

            result.Shipments ??= new List<Shipment>();
            bool isEmpty = result.Shipments.Count == 0;

            return ApiResponse<BranchShipmentsResponse>.Success(result, statusCode, isEmpty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure("Shipment retrieval was cancelled by the user.", 499);
        }
        catch (OperationCanceledException)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure(
                $"The request to {effectiveBase} timed out after {_httpClient.Timeout.TotalSeconds:0} seconds.",
                408);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure(
                $"Unable to reach the logistics service at {effectiveBase}: {ex.Message}",
                503);
        }
        catch (Exception ex)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure(
                $"Unexpected error during retrieval: {ex.Message}",
                500);
        }
    }

    private static string? TryExtractErrorMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var errProp))
            {
                return errProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
            {
                return msgProp.GetString();
            }
        }
        catch
        {
            // Ignore non-JSON error bodies
        }
        return null;
    }
}
