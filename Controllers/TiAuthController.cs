using Microsoft.AspNetCore.Mvc;
using TiTeamsWebhook.Models.TiApi;
using TiTeamsWebhook.Services;

namespace TiTeamsWebhook.Controllers
{
    /// <summary>
    /// Controller for TI API Authentication operations
    /// </summary>
    [ApiController]
    [Route("api/ti/auth")]
    [Tags("TI API Authentication")]
    public class TiAuthController : ControllerBase
    {
        private readonly ITiAuthService _authService;
        private readonly ILogger<TiAuthController> _logger;

        public TiAuthController(ITiAuthService authService, ILogger<TiAuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticate with TI API using configured credentials
        /// </summary>
        /// <returns>Authentication result</returns>
        /// <response code="200">Authentication successful</response>
        /// <response code="401">Authentication failed</response>
        /// <response code="500">Server error during authentication</response>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(TiAuthResult), 200)]
        [ProducesResponseType(typeof(TiAuthResult), 401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TiAuthResult>> AuthenticateAsync()
        {
            try
            {
                _logger.LogInformation("TI API authentication requested");

                var result = await _authService.AuthenticateAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("TI API authentication successful");

                    // Don't expose the actual token in the response for security
                    return Ok(new
                    {
                        isSuccess = result.IsSuccess,
                        message = "Authentication successful",
                        expiryTime = result.ExpiryTime,
                        tokenPreview = result.AccessToken?[..Math.Min(10, result.AccessToken.Length)] + "..."
                    });
                }

                _logger.LogWarning("TI API authentication failed: {ErrorMessage}", result.ErrorMessage);
                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TI API authentication");
                return StatusCode(500, new { error = "Internal server error during authentication" });
            }
        }

        /// <summary>
        /// Test authentication with custom credentials
        /// </summary>
        /// <param name="request">Test authentication request with custom credentials</param>
        /// <returns>Authentication result</returns>
        /// <response code="200">Authentication test completed</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Server error during authentication</response>
        [HttpPost("test")]
        [ProducesResponseType(typeof(TiAuthResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TiAuthResult>> TestAuthenticationAsync([FromBody] TestAuthRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("TI API authentication test requested for client: {ClientId}", request.ClientId);

                var result = await _authService.AuthenticateAsync(request.ClientId, request.ClientSecret);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("TI API authentication test successful");

                    // Don't store test credentials token, just return success status
                    return Ok(new
                    {
                        isSuccess = result.IsSuccess,
                        message = "Test authentication successful",
                        expiryTime = result.ExpiryTime,
                        note = "Token not stored - this was a test only"
                    });
                }

                _logger.LogWarning("TI API authentication test failed: {ErrorMessage}", result.ErrorMessage);
                return Ok(result); // Still return 200 OK for test results
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TI API authentication test");
                return StatusCode(500, new { error = "Internal server error during authentication test" });
            }
        }

        /// <summary>
        /// Get current authentication status
        /// </summary>
        /// <returns>Current authentication status</returns>
        /// <response code="200">Status retrieved successfully</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(AuthStatusResponse), 200)]
        public async Task<ActionResult<AuthStatusResponse>> GetAuthStatusAsync()
        {
            try
            {
                var status = await _authService.GetAuthStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting authentication status");
                return StatusCode(500, new { error = "Internal server error getting auth status" });
            }
        }

        /// <summary>
        /// Refresh the current authentication token
        /// </summary>
        /// <returns>Refresh result</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="401">Token refresh failed</response>
        [HttpPost("refresh")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> RefreshTokenAsync()
        {
            try
            {
                _logger.LogInformation("TI API token refresh requested");

                // Clear current token to force refresh
                _authService.ClearToken();

                var result = await _authService.AuthenticateAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("TI API token refreshed successfully");
                    return Ok(new
                    {
                        message = "Token refreshed successfully",
                        expiryTime = result.ExpiryTime
                    });
                }

                _logger.LogWarning("TI API token refresh failed: {ErrorMessage}", result.ErrorMessage);
                return Unauthorized(new { error = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TI API token refresh");
                return StatusCode(500, new { error = "Internal server error during token refresh" });
            }
        }

        /// <summary>
        /// Clear the current authentication token
        /// </summary>
        /// <returns>Clear result</returns>
        /// <response code="200">Token cleared successfully</response>
        [HttpPost("clear")]
        [ProducesResponseType(200)]
        public ActionResult ClearToken()
        {
            try
            {
                _authService.ClearToken();
                _logger.LogInformation("TI API token cleared");

                return Ok(new { message = "Authentication token cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception clearing authentication token");
                return StatusCode(500, new { error = "Internal server error clearing token" });
            }
        }

        /// <summary>
        /// Health check for authentication service
        /// </summary>
        /// <returns>Health check result</returns>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                service = "TI Authentication Service",
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}