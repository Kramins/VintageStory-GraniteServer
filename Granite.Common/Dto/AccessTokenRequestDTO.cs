using System.Text.Json.Serialization;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for exchanging an access token for a JWT bearer token.
/// </summary>
public record AccessTokenRequestDTO
{
    public Guid ServerId { get; init; }

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
}
