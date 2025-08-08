# TI Teams Webhook Service

A .NET Core Web API service that receives webhooks from TI Store API and forwards notifications to Microsoft Teams.

## Features

- ✅ Receives TI shipment and invoice webhooks
- ✅ Forwards formatted notifications to Teams channels
- ✅ Swagger UI for API testing
- ✅ Structured logging with Serilog
- ✅ Health check endpoint
- ✅ Test endpoints for development

## Setup

1. **Configure Teams Webhook URLs**:
   - Update ppsettings.Development.json with your Teams webhook URLs
   - For production, set environment variables:
     `
     Teams__ShipmentWebhookUrl=https://...
     Teams__InvoiceWebhookUrl=https://...
     `

2. **Run the application**:
   `ash
   dotnet run
   `

3. **Test the API**:
   - Open browser to https://localhost:7000/swagger
   - Or use the 	est.http file in VS Code/Visual Studio

## API Endpoints

- GET /webhooks/health - Health check
- POST /webhooks/ti/shipment - TI shipment webhook
- POST /webhooks/ti/invoice - TI invoice webhook  
- POST /webhooks/test/shipment - Test shipment notification
- POST /webhooks/test/invoice - Test invoice notification

## Configuration

### Teams Webhook URLs

1. Go to your Teams channel
2. Click "..." → Connectors → Incoming Webhook
3. Configure webhook and copy URL
4. Add URLs to configuration

### Environment Variables

`ash
Teams__ShipmentWebhookUrl=https://yourcompany.webhook.office.com/webhookb2/...
Teams__InvoiceWebhookUrl=https://yourcompany.webhook.office.com/webhookb2/...
`

## Deployment

### Azure App Service
`ash
dotnet publish -c Release
# Deploy to Azure App Service
`

### Docker
`ash
docker build -t ti-teams-webhook .
docker run -p 80:80 ti-teams-webhook
`

## Development

- Built with .NET 8.0
- Uses Serilog for logging
- Swagger UI for API documentation
- HTTP client for Teams integration

## Testing

Use the included 	est.http file to test all endpoints, or use Swagger UI at /swagger.
