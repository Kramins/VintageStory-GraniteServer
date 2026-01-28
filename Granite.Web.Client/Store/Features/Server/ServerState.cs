using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Server;

public record ServerState
{
    public List<ServerDTO> Servers { get; init; } = [];
    public string? SelectedServerId { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}
