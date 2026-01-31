namespace Granite.Common.Dto.JsonApi;

public record JsonApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public object? StackTrace { get; init; }
}
