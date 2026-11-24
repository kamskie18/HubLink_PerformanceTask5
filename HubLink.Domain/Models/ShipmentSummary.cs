namespace HubLink.Domain.Models;

public class ShipmentSummary
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int InTransit { get; set; }
    public int Delivered { get; set; }
    public int Exception { get; set; }

    public static ShipmentSummary FromShipments(IEnumerable<Shipment>? shipments)
    {
        if (shipments == null)
        {
            return new ShipmentSummary();
        }

        var list = shipments.ToList();
        return new ShipmentSummary
        {
            Total = list.Count,
            Pending = list.Count(s => string.Equals(s.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            InTransit = list.Count(s => string.Equals(s.Status, "InTransit", StringComparison.OrdinalIgnoreCase)),
            Delivered = list.Count(s => string.Equals(s.Status, "Delivered", StringComparison.OrdinalIgnoreCase)),
            Exception = list.Count(s => string.Equals(s.Status, "Exception", StringComparison.OrdinalIgnoreCase))
        };
    }
}
