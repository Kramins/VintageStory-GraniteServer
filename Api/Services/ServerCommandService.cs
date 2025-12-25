using System;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class ServerCommandService
{
    private readonly ICoreServerAPI _api;

    public ServerCommandService(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<string> ListUsersAsync()
    {
        var result = await ExecuteCommandAsync("list", new CmdArgs("clients"));
        return result?.StatusMessage ?? "Failed to retrieve player list";
    }

    public async Task<string> AutoSaveWorldAsync()
    {
        var result = await ExecuteCommandAsync("autosavenow", new CmdArgs());
        return result?.StatusMessage ?? "Failed to save world";
    }

    public async Task<string> AnnounceMessageAsync(string message)
    {
        var args = new CmdArgs([message]);
        var result = await ExecuteCommandAsync("announce", args);
        return result?.StatusMessage ?? "Failed to announce message";
    }

    private async Task<TextCommandResult?> ExecuteCommandAsync(string command, CmdArgs args)
    {
        var tcs = new TaskCompletionSource<TextCommandResult?>();

        _api.Event.EnqueueMainThreadTask(
            () =>
            {
                var commandHandler = _api.ChatCommands.Get(command);
                try
                {
                    commandHandler.Execute(
                        new TextCommandCallingArgs()
                        {
                            Caller = new Caller
                            {
                                Type = EnumCallerType.Console,
                                CallerRole = "admin",
                                CallerPrivileges = new string[] { "*" },
                                FromChatGroupId = GlobalConstants.ConsoleGroup,
                            },
                            RawArgs = args,
                        },
                        result =>
                        {
                            if (tcs.Task.IsCompleted)
                            {
                                return;
                            }
                            tcs.TrySetResult(result);
                        }
                    );
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            },
            "ExecuteServerCommand"
        );
        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return result;
    }

    public async Task<string> KickUserAsync(string playerName, string reason)
    {
        var args = new CmdArgs([playerName, reason]);
        var result = await ExecuteCommandAsync("kick", args);
        return result.StatusMessage ?? $"Failed to kick player {playerName}";
    }
}
