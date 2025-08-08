namespace TiTeamsWebhook.Models
{
    public class ShipmentWebhookRequest
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string WayBillNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string ShipDate { get; set; } = string.Empty;
        public string EstimatedDelivery { get; set; } = string.Empty;
        public ShipmentDetails? ShipmentDetails { get; set; }
    }

    public class InvoiceWebhookRequest
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string IssueDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty;
    }

    public class ShipmentDetails
    {
        public string Weight { get; set; } = string.Empty;
        public string Dimensions { get; set; } = string.Empty;
        public int Packages { get; set; }
    }

    public class WebhookResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Message { get; set; }
    }
}
