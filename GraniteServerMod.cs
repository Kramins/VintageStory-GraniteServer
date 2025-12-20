using System;
using System.Threading.Tasks;
using GraniteServer.Api;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo(
    "GraniteServerMod",
    Authors = new string[] { "Kramins" },
    Description = "Server Administration Tools and features",
    Version = "0.0.1"
)]

namespace GraniteServer
{
    public class GraniteServerMod : ModSystem
    {
        private WebApi? _webApi;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _webApi = new WebApi(api);
            _webApi.Initialize();
        }

        public override void Dispose() { }
    }
}
