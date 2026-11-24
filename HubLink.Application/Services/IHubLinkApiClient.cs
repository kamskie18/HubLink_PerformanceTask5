using HubLink.Application.Models;
using HubLink.Domain.Models;

namespace HubLink.Application.Services;

public interface IHubLinkApiClient
{
    Task<ApiResponse<BranchShipmentsResponse>> GetShipmentsAsync(
        string? branchCode = null,
        bool useTestEndpoint = false,
        string? testMode = null,
        CancellationToken cancellationToken = default);
}
