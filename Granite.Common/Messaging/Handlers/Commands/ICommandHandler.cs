using System;
using System.Threading.Tasks;

namespace GraniteServer.Messaging.Handlers.Commands;

public interface ICommandHandler
{
    Task Handle(object command);
}

public interface ICommandHandler<TCommand> : ICommandHandler
    where TCommand : class
{
    Task Handle(TCommand command);

    async Task ICommandHandler.Handle(object command) => await Handle((TCommand)command);
}
