using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Players;

// Fetch actions
public record FetchPlayersAction(string ServerId);
public record FetchPlayersSuccessAction(List<PlayerDTO> Players, string ServerId);
public record FetchPlayersFailureAction(string ErrorMessage);

// Smart loading actions
public record LoadPlayersIfNeededAction(string ServerId);
public record RefreshPlayersAction(string ServerId);
public record ClearPlayersAction;

// Select player action
public record SelectPlayerAction(string PlayerId);
public record SelectPlayerSuccessAction(PlayerDTO Player);
public record SelectPlayerFailureAction(string ErrorMessage);

// Clear error action
public record ClearPlayersErrorAction;
