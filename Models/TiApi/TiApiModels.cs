using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TiTeamsWebhook.Models.TiApi
{
    /// <summary>
    /// TI API Configuration Settings
    /// </summary>
    public class TiApiSettings
    {
        public string BaseUrl { get; set; } = "https://transact.ti.com";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = "https://transact.ti.com/oauth2/token";
        public string Scope { get; set; } = "api";
        public int TokenExpiryBufferMinutes { get; set; } = 5; // Refresh token 5 minutes before expiry
    }

    /// <summary>
    /// OAuth2 Token Request for TI API
    /// </summary>
    public class TiTokenRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = "client_credentials";

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "api";
    }

    /// <summary>
    /// OAuth2 Token Response from TI API
    /// </summary>
    public class TiTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Calculated expiry time based on current time + expires_in
        /// </summary>
        [JsonIgnore]
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Check if token is expired or will expire soon
        /// </summary>
        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiryTime;

        /// <summary>
        /// Check if token will expire within buffer time
        /// </summary>
        [JsonIgnore]
        public bool ShouldRefresh(int bufferMinutes = 5)
            => DateTime.UtcNow.AddMinutes(bufferMinutes) >= ExpiryTime;
    }

    /// <summary>
    /// TI API Error Response
    /// </summary>
    public class TiApiErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;

        [JsonPropertyName("error_uri")]
        public string ErrorUri { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authentication Result
    /// </summary>
    public class TiAuthResult
    {
        public bool IsSuccess { get; set; }
        public string? AccessToken { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }

        public static TiAuthResult Success(string accessToken, DateTime expiryTime)
            => new() { IsSuccess = true, AccessToken = accessToken, ExpiryTime = expiryTime };

        public static TiAuthResult Failure(string errorMessage, string? errorCode = null)
            => new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
    }

    /// <summary>
    /// Test Authentication Request DTO
    /// </summary>
    public class TestAuthRequest
    {
        [Required]
        [Display(Name = "Client ID")]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client Secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [Display(Name = "Custom Base URL (Optional)")]
        public string? CustomBaseUrl { get; set; }
    }

    /// <summary>
    /// Authentication Status Response
    /// </summary>
    public class AuthStatusResponse
    {
        public bool IsAuthenticated { get; set; }
        public string? TokenType { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public int? ExpiresInMinutes { get; set; }
        public bool ShouldRefresh { get; set; }
        public string? Scope { get; set; }
        public string Status => IsAuthenticated ? "Valid" : "Invalid";
    }
}