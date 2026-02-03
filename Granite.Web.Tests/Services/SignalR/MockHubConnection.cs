using Microsoft.AspNetCore.SignalR.Client;
using Moq;

namespace Granite.Web.Tests.Services.SignalR;

/// <summary>
/// Mock HubConnection for testing SignalR service.
/// </summary>
public class MockHubConnection : Mock<HubConnection>
{
    private readonly Dictionary<string, List<Delegate>> _handlers = new();

    public MockHubConnection() : base(new[] { typeof(string) }, new object[] { "http://localhost:5000/hub" })
    {
        // Setup common properties
        Setup(h => h.State).Returns(HubConnectionState.Connected);
        Setup(h => h.ConnectionId).Returns("test-connection-id");

        // Setup On method
        Setup(h => h.On(It.IsAny<string>(), It.IsAny<Type[]>(), It.IsAny<Func<IList<object?>, Task>>()))
            .Callback((string methodName, Type[] parameterTypes, Func<IList<object?>, Task> handler) =>
            {
                if (!_handlers.ContainsKey(methodName))
                {
                    _handlers[methodName] = new List<Delegate>();
                }
                _handlers[methodName].Add(handler);
            });

        // Setup generic On method
        Setup(h => h.On<object>(It.IsAny<string>(), It.IsAny<Func<object, Task>>()))
            .Callback((string methodName, Func<object, Task> handler) =>
            {
                if (!_handlers.ContainsKey(methodName))
                {
                    _handlers[methodName] = new List<Delegate>();
                }
                _handlers[methodName].Add(handler);
            });

        // Setup StartAsync
        Setup(h => h.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup StopAsync
        Setup(h => h.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup SendAsync
        Setup(h => h.SendAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup DisposeAsync
        Setup(h => h.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
    }

    public void InvokeServerMethod(string methodName, object data)
    {
        if (_handlers.TryGetValue(methodName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Func<object, Task> func)
                {
                    func.Invoke(data).GetAwaiter().GetResult();
                }
            }
        }
    }
}
