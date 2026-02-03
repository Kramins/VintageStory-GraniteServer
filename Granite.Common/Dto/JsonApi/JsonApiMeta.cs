namespace Granite.Common.Dto.JsonApi;

public record JsonApiMeta
{
    public PaginationMeta? Pagination { get; init; }
}
