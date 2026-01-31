using System.Collections.Generic;

namespace Granite.Common.Dto.JsonApi;

public record JsonApiDocument<T>
{
    public JsonApiDocument() { }

    public JsonApiDocument(T data)
    {
        Data = data;
    }

    public T? Data { get; init; }
    public JsonApiMeta? Meta { get; init; }
    public List<JsonApiError> Errors { get; init; } = new();
}
