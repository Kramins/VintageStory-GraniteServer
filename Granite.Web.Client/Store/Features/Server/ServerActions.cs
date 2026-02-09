using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Server;

// Fetch actions
public record FetchServersAction;
public record FetchServersSuccessAction(List<ServerDetailsDTO> Servers);
public record FetchServersFailureAction(string Error);

// Selection actions
public record SelectServerAction(string ServerId);

// Create server actions
public record CreateServerAction(CreateServerRequestDTO Request);
public record CreateServerSuccessAction(ServerDetailsDTO Server);
public record CreateServerFailureAction(string Error);

// Update server actions
public record UpdateServerAction(Guid ServerId, UpdateServerRequestDTO Request);
public record UpdateServerSuccessAction(ServerDetailsDTO Server);
public record UpdateServerFailureAction(string Error);

// Delete server actions
public record DeleteServerAction(Guid ServerId);
public record DeleteServerSuccessAction(Guid ServerId);
public record DeleteServerFailureAction(string Error);

// Get server details actions (renamed from FetchServerDetailsAction)
public record FetchServerDetailsAction(Guid ServerId);
public record FetchServerDetailsSuccessAction(ServerDetailsDTO Server);
public record FetchServerDetailsFailureAction(string Error);

// Get server config actions
public record FetchServerConfigAction(Guid ServerId);
public record FetchServerConfigSuccessAction(ServerConfigDTO Config);
public record FetchServerConfigFailureAction(string Error);

// Update server config actions
public record UpdateServerConfigAction(Guid ServerId, ServerConfigDTO Config);
public record UpdateServerConfigSuccessAction(ServerConfigDTO Config);
public record UpdateServerConfigFailureAction(string Error);

// Server control actions
public record RestartServerAction(Guid ServerId);
public record RestartServerSuccessAction;
public record RestartServerFailureAction(string Error);

public record StopServerAction(Guid ServerId);
public record StopServerSuccessAction;
public record StopServerFailureAction(string Error);

// Regenerate token actions
public record RegenerateTokenAction(Guid ServerId);
public record RegenerateTokenSuccessAction(TokenRegeneratedResponseDTO Response);
public record RegenerateTokenFailureAction(string Error);

// Clear error action
public record ClearServerErrorAction;
