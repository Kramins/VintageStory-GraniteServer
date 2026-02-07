namespace Granite.Common.Dto;

public class TokenRegeneratedResponseDTO
{
    public required Guid Id { get; set; }

    /// <summary>
    /// The newly generated API access token. This is only shown once.
    /// Store this securely as it cannot be retrieved later.
    /// The old token is now invalid.
    /// </summary>
    public required string AccessToken { get; set; }
}
