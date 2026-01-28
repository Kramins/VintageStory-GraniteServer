using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Server;

public record FetchServersAction;
public record FetchServersSuccessAction(List<ServerDTO> Servers);
public record FetchServersFailureAction(string Error);
public record SelectServerAction(string ServerId);
