namespace Granite.Common.Dto.JsonApi;

public record PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
    public int TotalCount { get; init; }
}
