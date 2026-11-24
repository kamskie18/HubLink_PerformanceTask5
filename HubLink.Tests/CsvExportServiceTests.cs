using HubLink.Application.Services;
using HubLink.Domain.Models;
using Xunit;

namespace HubLink.Tests;

public class CsvExportServiceTests
{
    private readonly CsvExportService _service = new();

    [Fact]
    public void GenerateCsv_WithValidShipments_ProducesCorrectHeaderAndEscapedData()
    {
        // Arrange
        var shipments = new List<Shipment>
        {
            new()
            {
                TrackingNumber = "HUB-1004",
                Origin = "Butuan, Agusan del Norte",
                Destination = "Depot \"North\"",
                Status = "Exception",
                LastUpdatedUtc = DateTimeOffset.Parse("2026-11-07T09:00:00Z")
            }
        };

        // Act
        var csv = _service.GenerateCsv(shipments);

        // Assert
        var lines = csv.Trim().Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Equal("TrackingNumber,Origin,Destination,Status,LastUpdatedUtc", lines[0]);

        // Verify RFC 4180 escaping
        // Line should be: HUB-1004,"Butuan, Agusan del Norte","Depot ""North""",Exception,2026-11-07T09:00:00Z
        Assert.Equal("HUB-1004,\"Butuan, Agusan del Norte\",\"Depot \"\"North\"\"\",Exception,2026-11-07T09:00:00Z", lines[1]);
    }

    [Fact]
    public void GenerateCsv_WithEmptyCollection_ThrowsInvalidOperationException()
    {
        // Act & Assert (AC-08 requirement: prevent producing empty/misleading report)
        Assert.Throws<InvalidOperationException>(() => _service.GenerateCsv(new List<Shipment>()));
        Assert.Throws<InvalidOperationException>(() => _service.GenerateCsv(null));
    }

    [Theory]
    [InlineData("NormalText", "NormalText")]
    [InlineData("Contains,Comma", "\"Contains,Comma\"")]
    [InlineData("Contains\"Quote", "\"Contains\"\"Quote\"")]
    [InlineData("Contains\nNewline", "\"Contains\nNewline\"")]
    public void EscapeCsv_HandlesSpecialCharactersProperly(string input, string expected)
    {
        var result = CsvExportService.EscapeCsv(input);
        Assert.Equal(expected, result);
    }
}
