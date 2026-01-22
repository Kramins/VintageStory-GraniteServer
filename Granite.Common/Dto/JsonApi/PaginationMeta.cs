namespace Granite.Common.Dto.JsonApi;

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}
