using System.Text.Json.Serialization;

namespace HubLink.Domain.Models;

public class BranchShipmentsResponse
{
    [JsonPropertyName("branchCode")]
    public string BranchCode { get; set; } = string.Empty;

    [JsonPropertyName("shipments")]
    public List<Shipment> Shipments { get; set; } = new();
}
