using System;
using System.Collections.Generic;
using GraniteServer.Data;
using Microsoft.Extensions.DependencyInjection;
using Vintagestory.API.Server;

namespace GraniteServer.Common;

public class VintageStoryProxyResolver : IVintageStoryProxyResolver
{
    private readonly GraniteDataContext _dataContext;
    private readonly IServiceProvider _services;

    public VintageStoryProxyResolver(GraniteDataContext dataContext, IServiceProvider services)
    {
        _dataContext = dataContext;
        _services = services;
    }

    public IVintageStoryDataProxy GetProxy(string? serverId = null)
    {
        // For now, we only support the local server.
        var localProxy = new LocalVintageStoryDataProxy(
            _services.GetRequiredService<ICoreServerAPI>()
        );

        return localProxy;
    }
}
