using HubLink.Domain.Models;

namespace HubLink.Application.Services;

public interface ICsvExportService
{
    string GenerateCsv(IEnumerable<Shipment>? shipments);
    Task ExportToFileAsync(IEnumerable<Shipment>? shipments, string filePath, CancellationToken cancellationToken = default);
}
