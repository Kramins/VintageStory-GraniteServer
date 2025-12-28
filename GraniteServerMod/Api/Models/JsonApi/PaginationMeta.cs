namespace GraniteServer.Api.Models.JsonApi;

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; internal set; }
}
