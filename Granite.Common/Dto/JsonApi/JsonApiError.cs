namespace Granite.Common.Dto.JsonApi;

public class JsonApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? StackTrace { get; set; }
}
