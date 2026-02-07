namespace Granite.Common.Dto;

public class ServerCreatedResponseDTO
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    /// The API access token for this server. This is only shown once upon creation.
    /// Store this securely as it cannot be retrieved later.
    /// </summary>
    public required string AccessToken { get; set; }
}
