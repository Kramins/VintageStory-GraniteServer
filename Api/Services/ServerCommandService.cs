using System;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class ServerCommandService
{
    private readonly ICoreAPI _api;

    public ServerCommandService(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    private async Task<TextCommandResult?> ExecuteCommandAsync(string command, CmdArgs args)
    {
        var tcs = new TaskCompletionSource<TextCommandResult?>();

        _api.ChatCommands.Execute(command, new TextCommandCallingArgs()
        {
            Caller = new Caller
            {
                Type = EnumCallerType.Console,
                CallerRole = "admin",
                CallerPrivileges = new string[] { "*" },
                FromChatGroupId = GlobalConstants.ConsoleGroup
            },
            RawArgs = args,
        }, (TextCommandResult result) =>
        {
            _api.Logger.Notification(result.StatusMessage);
            tcs.SetResult(result);
        });

        return await tcs.Task;
    }

    public async Task<string> ListUsersAsync()
    {
        var result = await ExecuteCommandAsync("list", new CmdArgs("clients"));
        return result?.StatusMessage ?? "Failed to retrieve player list";
    }

}
