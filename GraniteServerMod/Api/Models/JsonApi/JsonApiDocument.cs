using System.Collections.Generic;

namespace GraniteServer.Api.Models.JsonApi;

public class JsonApiDocument<T>
{
    public T? Data { get; set; }
    public JsonApiMeta? Meta { get; set; }
    public List<JsonApiError> Errors { get; set; } = new();
}
