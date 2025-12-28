using System;
using System.Text.Json;
using System.Threading.Tasks;
using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.ErrorHandling;
using GenHTTP.Modules.IO;
using GraniteServer.Api.Models.JsonApi;

namespace GraniteServer.Api.Handlers;

public sealed class JsonApiErrorMapper : IErrorMapper<Exception>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        WriteIndented = false,
    };

    public ValueTask<IResponse?> GetNotFound(IRequest request, IHandler handler)
    {
        var document = CreateDocument(
            ResponseStatus.NotFound,
            "not_found",
            "The requested resource was not found."
        );

        return new(BuildResponse(request, ResponseStatus.NotFound, document));
    }

    public ValueTask<IResponse?> Map(IRequest request, IHandler handler, Exception error)
    {
        var status = GetStatus(error);
        var document = CreateDocument(status, GetErrorCode(status), GetMessage(error));

        return new(BuildResponse(request, status, document));
    }

    private static JsonApiDocument<object?> CreateDocument(
        ResponseStatus status,
        string code,
        string message
    )
    {
        return new JsonApiDocument<object?>
        {
            Errors =
            {
                new JsonApiError { Code = code, Message = message },
            },
        };
    }

    private static IResponse BuildResponse(
        IRequest request,
        ResponseStatus status,
        JsonApiDocument<object?> document
    )
    {
        var payload = JsonSerializer.Serialize(document, SerializerOptions);

        return request
            .Respond()
            .Status(status)
            .Content(payload)
            .Type(ContentType.ApplicationJson)
            .Build();
    }

    private static ResponseStatus GetStatus(Exception error)
    {
        if (error is ProviderException providerException)
        {
            return providerException.Status;
        }

        return ResponseStatus.InternalServerError;
    }

    private static string GetMessage(Exception error)
    {
        if (error is ProviderException providerException)
        {
            return string.IsNullOrWhiteSpace(providerException.Message)
                ? "Request could not be processed"
                : providerException.Message;
        }

        return "An unexpected error occurred while processing the request.";
    }

    private static string GetErrorCode(ResponseStatus status)
    {
        return status switch
        {
            ResponseStatus.BadRequest => "bad_request",
            ResponseStatus.Unauthorized => "unauthorized",
            ResponseStatus.Forbidden => "forbidden",
            ResponseStatus.NotFound => "not_found",
            ResponseStatus.MethodNotAllowed => "method_not_allowed",
            ResponseStatus.Conflict => "conflict",
            ResponseStatus.InternalServerError => "internal_server_error",
            _ => status.ToString().ToLowerInvariant(),
        };
    }
}
