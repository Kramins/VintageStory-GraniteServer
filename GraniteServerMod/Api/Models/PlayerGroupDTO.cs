using System;

namespace GraniteServer.Api.Models;

public class PlayerGroupDTO
{
    public int Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string OwnerUID { get; set; } = string.Empty;
    public string Md5Identifier { get; set; } = string.Empty;
    public bool CreatedByPrivateMessage { get; set; }
    public string JoinPolicy { get; set; } = string.Empty;
}
