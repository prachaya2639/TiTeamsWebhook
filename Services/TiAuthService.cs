using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using TiTeamsWebhook.Models.TiApi;

namespace TiTeamsWebhook.Services
{
    /// <summary>
    /// Service for handling TI API Authentication using OAuth2 Client Credentials flow
    /// </summary>
    public interface ITiAuthService
    {
        Task<TiAuthResult> AuthenticateAsync();
        Task<TiAuthResult> AuthenticateAsync(string clientId, string clientSecret);
        Task<string?> GetValidTokenAsync();
        Task<AuthStatusResponse> GetAuthStatusAsync();
        bool IsTokenValid();
        void ClearToken();
    }

    public class TiAuthService : ITiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly TiApiSettings _settings;
        private readonly ILogger<TiAuthService> _logger;

        // In-memory token storage (for production, consider using distributed cache)
        private TiTokenResponse? _currentToken;
        private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

        public TiAuthService(
            HttpClient httpClient,
            IOptions<TiApiSettings> settings,
            ILogger<TiAuthService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticate using configured credentials
        /// </summary>
        public async Task<TiAuthResult> AuthenticateAsync()
        {
            return await AuthenticateAsync(_settings.ClientId, _settings.ClientSecret);
        }

        /// <summary>
        /// Authenticate using provided credentials
        /// </summary>
        public async Task<TiAuthResult> AuthenticateAsync(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return TiAuthResult.Failure("Client ID and Client Secret are required");
            }

            await _tokenSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Attempting TI API authentication for client: {ClientId}", clientId);

                var tokenRequest = new TiTokenRequest
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = _settings.Scope
                };

                // Create form-encoded content
                var formContent = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", tokenRequest.GrantType),
                    new("client_id", tokenRequest.ClientId),
                    new("client_secret", tokenRequest.ClientSecret),
                    new("scope", tokenRequest.Scope)
                };

                using var content = new FormUrlEncodedContent(formContent);
                content.Headers.ContentType = new("application/x-www-form-urlencoded");

                // Make the authentication request
                var response = await _httpClient.PostAsync(_settings.AuthUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<TiTokenResponse>(responseContent);
                    if (tokenResponse != null)
                    {
                        // Calculate expiry time
                        tokenResponse.ExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                        // Store the token
                        _currentToken = tokenResponse;

                        _logger.LogInformation("TI API authentication successful. Token expires at: {ExpiryTime}",
                            tokenResponse.ExpiryTime);

                        return TiAuthResult.Success(tokenResponse.AccessToken, tokenResponse.ExpiryTime);
                    }
                }

                // Handle error response
                var errorResponse = JsonSerializer.Deserialize<TiApiErrorResponse>(responseContent);
                var errorMessage = errorResponse?.ErrorDescription ?? $"Authentication failed with status: {response.StatusCode}";

                _logger.LogError("TI API authentication failed: {Error}", errorMessage);
                return TiAuthResult.Failure(errorMessage, errorResponse?.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TI API authentication");
                return TiAuthResult.Failure($"Authentication error: {ex.Message}");
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        /// <summary>
        /// Get a valid access token, refreshing if necessary
        /// </summary>
        public async Task<string?> GetValidTokenAsync()
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                // Check if we have a current token and it's still valid
                if (_currentToken != null && !_currentToken.ShouldRefresh(_settings.TokenExpiryBufferMinutes))
                {
                    return _currentToken.AccessToken;
                }

                // Token is expired or will expire soon, refresh it
                _logger.LogInformation("Token is expired or will expire soon, refreshing...");
                var authResult = await AuthenticateAsync();

                return authResult.IsSuccess ? authResult.AccessToken : null;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        /// <summary>
        /// Get current authentication status
        /// </summary>
        public async Task<AuthStatusResponse> GetAuthStatusAsync()
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                if (_currentToken == null)
                {
                    return new AuthStatusResponse { IsAuthenticated = false };
                }

                var minutesToExpiry = (int)(_currentToken.ExpiryTime - DateTime.UtcNow).TotalMinutes;

                return new AuthStatusResponse
                {
                    IsAuthenticated = !_currentToken.IsExpired,
                    TokenType = _currentToken.TokenType,
                    ExpiryTime = _currentToken.ExpiryTime,
                    ExpiresInMinutes = Math.Max(0, minutesToExpiry),
                    ShouldRefresh = _currentToken.ShouldRefresh(_settings.TokenExpiryBufferMinutes),
                    Scope = _currentToken.Scope
                };
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        /// <summary>
        /// Check if current token is valid
        /// </summary>
        public bool IsTokenValid()
        {
            return _currentToken != null && !_currentToken.IsExpired;
        }

        /// <summary>
        /// Clear stored token
        /// </summary>
        public void ClearToken()
        {
            _currentToken = null;
            _logger.LogInformation("TI API token cleared");
        }

        public void Dispose()
        {
            _tokenSemaphore?.Dispose();
        }
    }
}