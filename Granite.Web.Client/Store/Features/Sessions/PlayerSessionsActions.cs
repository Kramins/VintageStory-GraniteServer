using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Sessions;

public record LoadPlayerSessionsAction(
    string ServerId,
    string PlayerId,
    int Page = 1,
    int PageSize = 10,
    string? Sorts = null,
    string? Filters = null
);

public record LoadPlayerSessionsSuccessAction(
    List<PlayerSessionDTO> Sessions,
    int TotalItems,
    int Page,
    string ServerId,
    string PlayerId
);

public record LoadPlayerSessionsFailureAction(string ErrorMessage);

public record ClearPlayerSessionsAction;
