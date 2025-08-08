using Microsoft.AspNetCore.Mvc;
using TiTeamsWebhook.Models;
using TiTeamsWebhook.Services;

namespace TiTeamsWebhook.Controllers
{
    [ApiController]
    [Route("webhooks")]
    [Produces("application/json")]
    public class WebhookController : ControllerBase
    {
        private readonly ITeamsService _teamsService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ITeamsService teamsService, ILogger<WebhookController> logger)
        {
            _teamsService = teamsService;
            _logger = logger;
        }

        /// <summary>
        /// Receives shipment webhook from TI and forwards to Teams
        /// </summary>
        [HttpPost("ti/shipment")]
        public async Task<IActionResult> ReceiveShipmentWebhook([FromBody] ShipmentWebhookRequest request)
        {
            _logger.LogInformation("🚚 Shipment webhook received for order: {OrderNumber}", request.OrderNumber);

            try
            {
                var success = await _teamsService.SendShipmentNotificationAsync(request);

                return Ok(new WebhookResponse
                {
                    Status = success ? "success" : "error",
                    Message = success ? "Notification sent to Teams" : "Failed to send notification"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing shipment webhook");
                return StatusCode(500, new WebhookResponse
                {
                    Status = "error",
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Receives invoice webhook from TI and forwards to Teams
        /// </summary>
        [HttpPost("ti/invoice")]
        public async Task<IActionResult> ReceiveInvoiceWebhook([FromBody] InvoiceWebhookRequest request)
        {
            _logger.LogInformation("📄 Invoice webhook received for order: {OrderNumber}", request.OrderNumber);

            try
            {
                var success = await _teamsService.SendInvoiceNotificationAsync(request);

                return Ok(new WebhookResponse
                {
                    Status = success ? "success" : "error",
                    Message = success ? "Notification sent to Teams" : "Failed to send notification"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invoice webhook");
                return StatusCode(500, new WebhookResponse
                {
                    Status = "error",
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }

        /// <summary>
        /// Test shipment notification
        /// </summary>
        [HttpPost("test/shipment")]
        public async Task<IActionResult> TestShipmentNotification()
        {
            _logger.LogInformation("Testing shipment notification");
            var success = await _teamsService.SendTestShipmentAsync();
            return Ok(new { TestSent = success, Type = "Shipment", Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Test invoice notification
        /// </summary>
        [HttpPost("test/invoice")]
        public async Task<IActionResult> TestInvoiceNotification()
        {
            _logger.LogInformation("Testing invoice notification");
            var success = await _teamsService.SendTestInvoiceAsync();
            return Ok(new { TestSent = success, Type = "Invoice", Timestamp = DateTime.UtcNow });
        }
    }
}