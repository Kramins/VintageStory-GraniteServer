using System;
using System.Reflection;
using System.Threading.Tasks;
using GraniteServer.Api;
using GraniteServerMod.Data;
using Microsoft.EntityFrameworkCore;
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
        private readonly string _modConfigFileName = "graniteserverconfig.json";
        private WebApi? _webApi;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }

        private void OverrideConfigWithEnvironmentVariables(
            GraniteServerConfig config,
            ICoreServerAPI api
        )
        {
            foreach (
                var property in typeof(GraniteServerConfig).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance
                )
            )
            {
                string envVarName = $"GS_{property.Name.ToUpper()}";
                string? envVarValue = Environment.GetEnvironmentVariable(envVarName);

                if (!string.IsNullOrEmpty(envVarValue))
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(envVarValue, property.PropertyType);
                        property.SetValue(config, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        api.Logger.Warning(
                            $"Failed to set property {property.Name} from environment variable {envVarName}: {ex.Message}"
                        );
                    }
                }
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            var config = api.LoadModConfig<GraniteServerConfig>(_modConfigFileName);
            if (config == null)
            {
                config = new GraniteServerConfig();
            }

            OverrideConfigWithEnvironmentVariables(config, api);

            api.StoreModConfig<GraniteServerConfig>(config, _modConfigFileName);

            var logger = Mod.Logger;
            _webApi = new WebApi(api, config, logger);
            _webApi.Initialize();
        }

        public override void Dispose() { }
    }
}
