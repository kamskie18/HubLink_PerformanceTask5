using HubLink.Application.Configuration;
using HubLink.Application.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HubLink.Tests;

public class HubLinkApiClientTests
{
    private readonly HubLinkApiClient _client;

    public HubLinkApiClientTests()
    {
        var options = Options.Create(new HubLinkOptions
        {
            BaseAddress = "http://127.0.0.1:5080",
            BranchCode = "BR-001",
            TimeoutSeconds = 5
        });

        var httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5080/") };
        _client = new HubLinkApiClient(httpClient, options);
    }

    [Fact]
    public async Task GetShipmentsAsync_NormalBranchBR001_Returns5Items()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-001");

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.False(response.IsEmpty);
        Assert.NotNull(response.Data);
        Assert.Equal("BR-001", response.Data.BranchCode);
        Assert.Equal(5, response.Data.Shipments.Count);
    }

    [Fact]
    public async Task GetShipmentsAsync_ValidEmptyBranchBR999_ReturnsEmptySuccess()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-999");

        // Assert (OR-07: A successful response containing no shipments must be treated as an empty result rather than a failure)
        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.True(response.IsEmpty);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data.Shipments);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeBadRequest_ReturnsFailure()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "bad-request");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Simulated bad request", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeServerError_ReturnsFailure()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "server-error");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Simulated server error", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeInvalidJson_ReturnsMalformedJsonFailure()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "invalid-json");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Contains("malformed or incomplete JSON", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeDelayWithShortTimeout_ReturnsTimeoutFailure()
    {
        // Arrange: 1-second timeout against 10-second delay endpoint
        var shortOptions = Options.Create(new HubLinkOptions
        {
            BaseAddress = "http://127.0.0.1:5080",
            BranchCode = "BR-001",
            TimeoutSeconds = 1
        });
        var timeoutClient = new HubLinkApiClient(new HttpClient(), shortOptions);

        // Act
        var response = await timeoutClient.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "delay");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(408, response.StatusCode);
        Assert.Contains("timed out", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_UnreachableServer_ReturnsServiceUnavailableFailure()
    {
        // Act: use runtime baseAddress override pointing to unused local port
        var response = await _client.GetShipmentsAsync("BR-001", baseAddress: "http://127.0.0.1:59999");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(503, response.StatusCode);
        Assert.Contains("Unable to reach", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_InvalidBaseAddress_ReturnsBadRequestFailure()
    {
        // Act
        var response = await _client.GetShipmentsAsync("BR-001", baseAddress: "not-a-valid-url");

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid service base URL", response.ErrorMessage);
    }
}
