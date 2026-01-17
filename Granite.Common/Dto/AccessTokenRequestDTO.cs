using System.Text.Json.Serialization;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for exchanging an access token for a JWT bearer token.
/// </summary>
public class AccessTokenRequestDTO
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}
