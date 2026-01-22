using System.Text.Json.Serialization;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for exchanging an access token for a JWT bearer token.
/// </summary>
public class AccessTokenRequestDTO
{
    public Guid ServerId { get; set; }

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}
