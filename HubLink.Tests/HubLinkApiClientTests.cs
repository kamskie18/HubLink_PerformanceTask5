using System.Net;
using System.Text;
using HubLink.Application.Configuration;
using HubLink.Application.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HubLink.Tests;

public class HubLinkApiClientTests
{
    private static HubLinkApiClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler, int timeoutSeconds = 5)
    {
        var options = Options.Create(new HubLinkOptions
        {
            BaseAddress = "http://127.0.0.1:5080",
            BranchCode = "BR-001",
            TimeoutSeconds = timeoutSeconds
        });

        var httpClient = new HttpClient(new StubHttpMessageHandler(handler));
        return new HubLinkApiClient(httpClient, options);
    }

    [Fact]
    public async Task GetShipmentsAsync_NormalBranchBR001_Returns5Items()
    {
        var json = """
        {
          "branchCode": "BR-001",
          "shipments": [
            {"trackingNumber":"HUB-1001","branchCode":"BR-001","origin":"Cagayan de Oro","destination":"Valencia","status":"InTransit","lastUpdatedUtc":"2026-11-10T08:30:00Z"},
            {"trackingNumber":"HUB-1002","branchCode":"BR-001","origin":"Iligan","destination":"Malaybalay","status":"Pending","lastUpdatedUtc":"2026-11-09T13:15:00Z"},
            {"trackingNumber":"HUB-1003","branchCode":"BR-001","origin":"Valencia","destination":"Cagayan de Oro","status":"Delivered","lastUpdatedUtc":"2026-11-08T06:45:00Z"},
            {"trackingNumber":"HUB-1004","branchCode":"BR-001","origin":"Butuan, Agusan del Norte","destination":"Depot \"North\"","status":"Exception","lastUpdatedUtc":"2026-11-07T09:00:00Z"},
            {"trackingNumber":"HUB-1005","branchCode":"BR-001","origin":"Manolo Fortich","destination":"Valencia","status":"InTransit","lastUpdatedUtc":"2026-11-06T12:00:00Z"}
          ]
        }
        """;

        Uri? capturedUri = null;
        var client = CreateClient((req, _) =>
        {
            capturedUri = req.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var response = await client.GetShipmentsAsync("BR-001");

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.False(response.IsEmpty);
        Assert.NotNull(response.Data);
        Assert.Equal("BR-001", response.Data.BranchCode);
        Assert.Equal(5, response.Data.Shipments.Count);
        Assert.Equal("http://127.0.0.1:5080/api/shipments?branchCode=BR-001", capturedUri?.ToString());
    }

    [Fact]
    public async Task GetShipmentsAsync_ValidEmptyBranchBR999_ReturnsEmptySuccess()
    {
        var json = """{"branchCode":"BR-999","shipments":[]}""";
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

        var response = await client.GetShipmentsAsync("BR-999");

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.True(response.IsEmpty);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data.Shipments);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeBadRequest_ReturnsFailure()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"Simulated bad request."}""", Encoding.UTF8, "application/json")
        }));

        var response = await client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "bad-request");

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Simulated bad request", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeServerError_ReturnsFailure()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("""{"error":"Simulated server error."}""", Encoding.UTF8, "application/json")
        }));

        var response = await client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "server-error");

        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Simulated server error", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeInvalidJson_ReturnsMalformedJsonFailure()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"branchCode":"BR-001","shipments":[{"trackingNumber":""", Encoding.UTF8, "application/json")
        }));

        var response = await client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "invalid-json");

        Assert.False(response.IsSuccess);
        Assert.Contains("malformed or incomplete JSON", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_TestModeDelayWithShortTimeout_ReturnsTimeoutFailure()
    {
        var client = CreateClient(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, timeoutSeconds: 1);

        var response = await client.GetShipmentsAsync("BR-001", useTestEndpoint: true, testMode: "delay");

        Assert.False(response.IsSuccess);
        Assert.Equal(408, response.StatusCode);
        Assert.Contains("timed out", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_UnreachableServer_ReturnsServiceUnavailableFailure()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("Connection refused"));

        var response = await client.GetShipmentsAsync("BR-001", baseAddress: "http://127.0.0.1:59999");

        Assert.False(response.IsSuccess);
        Assert.Equal(503, response.StatusCode);
        Assert.Contains("Unable to reach", response.ErrorMessage);
    }

    [Fact]
    public async Task GetShipmentsAsync_InvalidBaseAddress_ReturnsBadRequestFailure()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var response = await client.GetShipmentsAsync("BR-001", baseAddress: "not-a-valid-url");

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid service base URL", response.ErrorMessage);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
