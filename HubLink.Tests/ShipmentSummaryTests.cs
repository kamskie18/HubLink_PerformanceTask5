using HubLink.Domain.Models;
using Xunit;

namespace HubLink.Tests;

public class ShipmentSummaryTests
{
    [Fact]
    public void FromShipments_WithBR001Fixtures_CalculatesAccurateCounts()
    {
        // Arrange: Section 3 BR-001 fixture dataset (Total 5: Pending 1, InTransit 2, Delivered 1, Exception 1)
        var shipments = new List<Shipment>
        {
            new() { TrackingNumber = "HUB-1001", Status = "InTransit" },
            new() { TrackingNumber = "HUB-1002", Status = "Pending" },
            new() { TrackingNumber = "HUB-1003", Status = "Delivered" },
            new() { TrackingNumber = "HUB-1004", Status = "Exception" },
            new() { TrackingNumber = "HUB-1005", Status = "InTransit" },
        };

        // Act
        var summary = ShipmentSummary.FromShipments(shipments);

        // Assert
        Assert.Equal(5, summary.Total);
        Assert.Equal(1, summary.Pending);
        Assert.Equal(2, summary.InTransit);
        Assert.Equal(1, summary.Delivered);
        Assert.Equal(1, summary.Exception);
    }

    [Fact]
    public void FromShipments_WithNullOrEmpty_ReturnsZeros()
    {
        var summaryNull = ShipmentSummary.FromShipments(null);
        var summaryEmpty = ShipmentSummary.FromShipments(new List<Shipment>());

        Assert.Equal(0, summaryNull.Total);
        Assert.Equal(0, summaryEmpty.Total);
        Assert.Equal(0, summaryNull.Pending);
        Assert.Equal(0, summaryNull.InTransit);
        Assert.Equal(0, summaryNull.Delivered);
        Assert.Equal(0, summaryNull.Exception);
    }
}
