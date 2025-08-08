using System.Text;
using System.Text.Json;
using TiTeamsWebhook.Models;

namespace TiTeamsWebhook.Services
{
    public interface ITeamsService
    {
        Task<bool> SendShipmentNotificationAsync(ShipmentWebhookRequest shipment);
        Task<bool> SendInvoiceNotificationAsync(InvoiceWebhookRequest invoice);
        Task<bool> SendTestShipmentAsync();
        Task<bool> SendTestInvoiceAsync();
    }

    public class TeamsService : ITeamsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TeamsService> _logger;

        public TeamsService(HttpClient httpClient, IConfiguration configuration, ILogger<TeamsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendShipmentNotificationAsync(ShipmentWebhookRequest shipment)
        {
            var message = CreateShipmentMessage(shipment);
            var webhookUrl = _configuration["Teams:ShipmentWebhookUrl"];

            // เพิ่มบรรทัดนี้เพื่อ debug
            _logger.LogInformation("🔍 Webhook URL from config: {WebhookUrl}", webhookUrl ?? "NULL");

            return await SendToTeamsAsync(webhookUrl, message);
        }

        public async Task<bool> SendInvoiceNotificationAsync(InvoiceWebhookRequest invoice)
        {
            var message = CreateInvoiceMessage(invoice);
            var webhookUrl = _configuration["Teams:InvoiceWebhookUrl"];
            return await SendToTeamsAsync(webhookUrl, message);
        }

        public async Task<bool> SendTestShipmentAsync()
        {
            var testData = new ShipmentWebhookRequest
            {
                OrderNumber = "123456789",
                WayBillNumber = "ASN001",
                TrackingNumber = "1Z999AA1234567890",
                Carrier = "UPS",
                ShipDate = "2024-08-15",
                EstimatedDelivery = "2024-08-17"
            };

            return await SendShipmentNotificationAsync(testData);
        }

        public async Task<bool> SendTestInvoiceAsync()
        {
            var testData = new InvoiceWebhookRequest
            {
                OrderNumber = "123456789",
                DocumentNumber = "INV-2024-001",
                TotalAmount = 35.94m,
                IssueDate = "2024-08-16",
                DueDate = "2024-09-15",
                PdfUrl = "https://ti.com/invoices/pdf/123456789"
            };

            return await SendInvoiceNotificationAsync(testData);
        }

        private TeamsMessageCard CreateShipmentMessage(ShipmentWebhookRequest shipment)
        {
            var actions = new List<TeamsPotentialAction>();
            
            if (!string.IsNullOrEmpty(shipment.TrackingNumber))
            {
                actions.Add(new TeamsPotentialAction
                {
                    Type = "OpenUri",
                    Name = "🔍 Track Package",
                    Targets = new List<TeamsTarget>
                    {
                        new() { Os = "default", Uri = GetTrackingUrl(shipment.Carrier, shipment.TrackingNumber) }
                    }
                });
            }

            return new TeamsMessageCard
            {
                ThemeColor = "0078D4",
                Summary = "TI Order Shipped",
                Sections = new List<TeamsSection>
                {
                    new()
                    {
                        ActivityTitle = "🚚 Order Shipped Successfully",
                        ActivitySubtitle = $"Order #{shipment.OrderNumber}",
                        ActivityImage = "https://img.icons8.com/color/48/shipped.png",
                        Facts = new List<TeamsFact>
                        {
                            new() { Name = "📦 Tracking Number", Value = shipment.TrackingNumber ?? "N/A" },
                            new() { Name = "🚛 Carrier", Value = shipment.Carrier ?? "N/A" },
                            new() { Name = "📅 Ship Date", Value = shipment.ShipDate ?? "N/A" },
                            new() { Name = "🎯 Est. Delivery", Value = shipment.EstimatedDelivery ?? "N/A" },
                            new() { Name = "📋 Way Bill", Value = shipment.WayBillNumber ?? "N/A" }
                        }
                    }
                },
                PotentialAction = actions.Any() ? actions : null
            };
        }

        private TeamsMessageCard CreateInvoiceMessage(InvoiceWebhookRequest invoice)
        {
            var actions = new List<TeamsPotentialAction>();

            if (!string.IsNullOrEmpty(invoice.PdfUrl))
            {
                actions.Add(new TeamsPotentialAction
                {
                    Type = "OpenUri",
                    Name = "📥 Download PDF",
                    Targets = new List<TeamsTarget>
                    {
                        new() { Os = "default", Uri = invoice.PdfUrl }
                    }
                });
            }

            return new TeamsMessageCard
            {
                ThemeColor = "00AA44",
                Summary = "TI Invoice Ready",
                Sections = new List<TeamsSection>
                {
                    new()
                    {
                        ActivityTitle = "📄 Invoice Available",
                        ActivitySubtitle = $"Order #{invoice.OrderNumber}",
                        ActivityImage = "https://img.icons8.com/color/48/invoice.png",
                        Facts = new List<TeamsFact>
                        {
                            new() { Name = "📄 Invoice Number", Value = invoice.DocumentNumber },
                            new() { Name = "💰 Total Amount", Value = $"{invoice.Currency} {invoice.TotalAmount:F2}" },
                            new() { Name = "📅 Issue Date", Value = invoice.IssueDate ?? "N/A" },
                            new() { Name = "💳 Due Date", Value = invoice.DueDate ?? "N/A" }
                        }
                    }
                },
                PotentialAction = actions.Any() ? actions : null
            };
        }

        private async Task<bool> SendToTeamsAsync(string? webhookUrl, TeamsMessageCard message)
        {
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogError("Teams webhook URL is not configured");
                return false;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(message, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation("Sending to Teams: {Json}", json);
                
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Message sent to Teams successfully");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to send to Teams: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception while sending to Teams");
                return false;
            }
        }

        private static string GetTrackingUrl(string carrier, string trackingNumber)
        {
            return carrier?.ToUpper() switch
            {
                "UPS" => $"https://www.ups.com/track?tracknum={trackingNumber}",
                "FEDEX" => $"https://www.fedex.com/fedextrack/?trknbr={trackingNumber}",
                "DHL" => $"https://www.dhl.com/en/express/tracking.html?AWB={trackingNumber}",
                _ => $"https://www.google.com/search?q=track+{trackingNumber}"
            };
        }
    }
}
