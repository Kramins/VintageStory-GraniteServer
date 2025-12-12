using System;
using System.Threading.Tasks;
using GraniteServer.Api;
using GraniteServer.Rcon;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo("GraniteServerMod",
        Authors = new string[] { "Kramins" },
        Description = "Server Administration Tools and features",
        Version = "1.0.0")]

namespace GraniteServer
{

    public class GraniteServerMod : ModSystem
    {
        internal const string ConfigFile = "vsrcon.json";
        internal ICoreServerAPI Api = null!;
        internal static VintageStoryRconConfig Config { get; set; } = null!;
        public static VintageStoryRconServer? VRCon { get; private set; }
        private WebApi? _webApi;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Api = api;

            _webApi = new WebApi(Api);
            _webApi.Initialize();

        }

        public override void Dispose()
        {


            VRCon?.Dispose();
            VRCon = null;
        }
    }
}

