using System.Text;
using HubLink.Domain.Models;

namespace HubLink.Application.Services;

public class CsvExportService : ICsvExportService
{
    public const string Header = "TrackingNumber,Origin,Destination,Status,LastUpdatedUtc";

    public string GenerateCsv(IEnumerable<Shipment>? shipments)
    {
        if (shipments == null)
        {
            throw new InvalidOperationException("Cannot generate CSV: shipment collection is null.");
        }

        var list = shipments.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("No shipment records are available for export.");
        }

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var item in list)
        {
            var line = string.Join(",",
                EscapeCsv(item.TrackingNumber),
                EscapeCsv(item.Origin),
                EscapeCsv(item.Destination),
                EscapeCsv(item.Status),
                EscapeCsv(item.FormattedUtcTime)
            );
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    public async Task ExportToFileAsync(IEnumerable<Shipment>? shipments, string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Export file path must not be empty.", nameof(filePath));
        }

        var content = GenerateCsv(shipments);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
    }

    public static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Check if escaping is necessary according to RFC 4180
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
