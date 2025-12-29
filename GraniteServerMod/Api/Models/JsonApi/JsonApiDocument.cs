using System.Collections.Generic;

namespace GraniteServer.Api.Models.JsonApi;

public class JsonApiDocument<T>
{
    public JsonApiDocument() { }

    public JsonApiDocument(T data)
    {
        Data = data;
    }

    public T? Data { get; set; }
    public JsonApiMeta? Meta { get; set; }
    public List<JsonApiError> Errors { get; set; } = new();
}
