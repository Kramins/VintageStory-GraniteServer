namespace Granite.Common.Dto;

public record BasicAuthCredentialsDTO
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
