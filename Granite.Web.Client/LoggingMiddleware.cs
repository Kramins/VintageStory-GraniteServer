using System;
using Fluxor;

namespace Granite.Web.Client;

public class LoggingMiddleware : Middleware
{
    public override Task InitializeAsync(IDispatcher dispatcher, IStore store)
    {
        Console.WriteLine(nameof(InitializeAsync));
        return Task.CompletedTask;
    }

    public override void AfterDispatch(object action)
    {
        Console.WriteLine($"Action Dispatched: {action.GetType().Name}");
    }
}
