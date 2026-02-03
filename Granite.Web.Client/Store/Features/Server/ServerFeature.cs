using Fluxor;

namespace Granite.Web.Client.Store.Features.Server;

public class ServerFeature : Feature<ServerState>
{
    public override string GetName() => "Server";

    protected override ServerState GetInitialState()
    {
        return new ServerState
        {
            Servers = [],
            SelectedServerId = null,
            IsLoading = false,
            Error = null
        };
    }
}
