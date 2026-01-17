using System.Threading.Tasks;
using GraniteServer.Messaging.Events;

namespace GraniteServer.Messaging.Handlers.Events;

public interface IEventHandler
{
    Task Handle(object command);
}

public interface IEventHandler<TEvent> : IEventHandler
    where TEvent : EventMessage
{
    Task Handle(TEvent command);

    async Task IEventHandler.Handle(object command) => await Handle((TEvent)command);
}
