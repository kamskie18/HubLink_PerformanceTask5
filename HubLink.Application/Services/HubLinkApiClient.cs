using System.Net.Http.Json;
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

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseAddress))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseAddress.TrimEnd('/') + "/");
        }

        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }
    }

    public async Task<ApiResponse<BranchShipmentsResponse>> GetShipmentsAsync(
        string? branchCode = null,
        bool useTestEndpoint = false,
        string? testMode = null,
        CancellationToken cancellationToken = default)
    {
        var targetBranch = string.IsNullOrWhiteSpace(branchCode) ? _options.BranchCode : branchCode.Trim();

        string requestUri;
        if (useTestEndpoint)
        {
            var mode = string.IsNullOrWhiteSpace(testMode) ? (_options.TestMode ?? "normal") : testMode.Trim();
            requestUri = $"api/test/shipments?branchCode={Uri.EscapeDataString(targetBranch)}&mode={Uri.EscapeDataString(mode)}";
        }
        else
        {
            requestUri = $"api/shipments?branchCode={Uri.EscapeDataString(targetBranch)}";
        }

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMsg = TryExtractErrorMessage(errorBody) ?? $"Service returned HTTP {statusCode} ({response.ReasonPhrase})";
                return ApiResponse<BranchShipmentsResponse>.Failure(errorMsg, statusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<BranchShipmentsResponse>.Failure("Service returned an empty response body.", statusCode);
            }

            BranchShipmentsResponse? result;
            try
            {
                result = JsonSerializer.Deserialize<BranchShipmentsResponse>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                return ApiResponse<BranchShipmentsResponse>.Failure($"Unable to interpret service response: malformed or incomplete JSON. {ex.Message}", statusCode);
            }

            if (result == null)
            {
                return ApiResponse<BranchShipmentsResponse>.Failure("Parsed response was null.", statusCode);
            }

            bool isEmpty = result.Shipments == null || result.Shipments.Count == 0;
            result.Shipments ??= new List<Shipment>();

            return ApiResponse<BranchShipmentsResponse>.Success(result, statusCode, isEmpty);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure("The request to the logistics service timed out.", 408);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure($"Unable to connect to the external logistics service: {ex.Message}", 503);
        }
        catch (Exception ex)
        {
            return ApiResponse<BranchShipmentsResponse>.Failure($"An unexpected error occurred during service retrieval: {ex.Message}", 500);
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
            // not valid JSON, ignore and return null
        }
        return null;
    }
}
