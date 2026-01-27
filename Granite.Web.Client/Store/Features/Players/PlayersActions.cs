using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Players;

// Fetch actions
public record FetchPlayersAction;
public record FetchPlayersSuccessAction(List<PlayerDTO> Players);
public record FetchPlayersFailureAction(string ErrorMessage);

// Select player action
public record SelectPlayerAction(string PlayerId);
public record SelectPlayerSuccessAction(PlayerDTO Player);
public record SelectPlayerFailureAction(string ErrorMessage);

// Clear error action
public record ClearPlayersErrorAction;
