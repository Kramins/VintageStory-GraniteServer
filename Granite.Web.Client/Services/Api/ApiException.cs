namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Exception thrown when an API call fails.
/// </summary>
public class ApiException : Exception
{
    public int? StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ApiException(string message, int statusCode, string? errorCode = null, string? errorMessage = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
