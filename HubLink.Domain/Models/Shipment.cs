using System.Text.Json.Serialization;

namespace HubLink.Domain.Models;

public class Shipment
{
    [JsonPropertyName("trackingNumber")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("branchCode")]
    public string BranchCode { get; set; } = string.Empty;

    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdatedUtc")]
    public DateTimeOffset LastUpdatedUtc { get; set; }

    /// <summary>
    /// Formatted local timestamp for display in UI.
    /// </summary>
    public string FormattedLocalTime => LastUpdatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// ISO 8601 UTC string for CSV output.
    /// </summary>
    public string FormattedUtcTime => LastUpdatedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
}
