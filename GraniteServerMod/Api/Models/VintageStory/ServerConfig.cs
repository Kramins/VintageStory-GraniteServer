using System;
using System.Collections.Generic;

namespace GraniteServer.Api.Models.VintageStory;

public class ServerConfigFile
{
    public List<ServerConfigRole> Roles { get; set; } = new List<ServerConfigRole>();
}
