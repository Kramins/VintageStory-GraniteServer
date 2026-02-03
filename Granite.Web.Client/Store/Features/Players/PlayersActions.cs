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

// Fetch player details actions
public record FetchPlayerDetailsAction(string ServerId, string PlayerUid);

public record FetchPlayerDetailsSuccessAction(PlayerDetailsDTO PlayerDetails);

public record FetchPlayerDetailsFailureAction(string ErrorMessage);

// Player update actions
public record UpdatePlayerConnectionStateAction(
    string PlayerUID,
    Guid ServerId,
    string ConnectionState,
    string Name,
    string IpAddress
);

public record UpdatePlayerBanStatusAction(
    string PlayerUID,
    Guid ServerId,
    bool IsBanned,
    string? BanReason = null,
    string? Name = null
);

public record UpdatePlayerWhitelistStatusAction(
    string PlayerUID,
    Guid ServerId,
    bool IsWhitelisted,
    string? Name = null
);
