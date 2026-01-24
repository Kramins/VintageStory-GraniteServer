using System;
using System.Threading.Tasks;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Mod.Handlers.Commands;

public class ServerCommandHandlers : ICommandHandler<AnnounceMessageCommand>
{
    private readonly ServerCommandService _commandService;
    private readonly ILogger _logger;

    public ServerCommandHandlers(ServerCommandService commandService, ILogger logger)
    {
        _commandService = commandService;
        _logger = logger;
    }

    async Task ICommandHandler<AnnounceMessageCommand>.Handle(AnnounceMessageCommand command)
    {
        try
        {
            var message = command.Data?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.Warning("[ServerCommands] Received AnnounceMessageCommand with empty message");
                return;
            }

            // Use the ServerCommandService to execute the announce command
            var result = await _commandService.AnnounceMessageAsync(message);
            _logger.Notification($"[ServerCommands] Announced message: {message} - Result: {result}");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerCommands] Failed to announce message: {ex.Message}");
        }
    }
}
