namespace GraniteServer.Common;

public interface IVintageStoryProxyResolver
{
    IVintageStoryDataProxy GetProxy(string? serverId = null);
}
