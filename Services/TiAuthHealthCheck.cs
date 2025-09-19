using Microsoft.Extensions.Diagnostics.HealthChecks;
using TiTeamsWebhook.Services;

namespace TiTeamsWebhook.Services
{
    /// <summary>
    /// Health check for TI Authentication Service
    /// </summary>
    public class TiAuthHealthCheck : IHealthCheck
    {
        private readonly ITiAuthService _authService;
        private readonly ILogger<TiAuthHealthCheck> _logger;

        public TiAuthHealthCheck(ITiAuthService authService, ILogger<TiAuthHealthCheck> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check authentication status
                var status = await _authService.GetAuthStatusAsync();

                var data = new Dictionary<string, object>
                {
                    ["isAuthenticated"] = status.IsAuthenticated,
                    ["shouldRefresh"] = status.ShouldRefresh,
                    ["expiresInMinutes"] = status.ExpiresInMinutes ?? 0
                };

                if (status.IsAuthenticated && !status.ShouldRefresh)
                {
                    return HealthCheckResult.Healthy("TI Authentication is healthy", data);
                }
                else if (status.IsAuthenticated && status.ShouldRefresh)
                {
                    return HealthCheckResult.Degraded("TI Authentication token needs refresh", data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy("TI Authentication is not authenticated", data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking TI Auth health");

                return HealthCheckResult.Unhealthy(
                    "TI Authentication health check failed",
                    ex,
                    new Dictionary<string, object> { ["error"] = ex.Message });
            }
        }
    }
}