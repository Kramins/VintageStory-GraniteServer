using System;
using System.Collections.Generic;

namespace GraniteServer.Messaging.Commands;

public class SyncPlayerModerationDataCommand : CommandMessage<SyncPlayerModerationDataCommandData> { }

public class SyncPlayerModerationDataCommandData
{
    public List<PlayerModerationRecord> Players { get; set; } = new();
}

public class PlayerModerationRecord
{
    public string PlayerUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // Ban data
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public string? BanBy { get; set; }
    public DateTime? BanUntil { get; set; }
    
    // Whitelist data
    public bool IsWhitelisted { get; set; }
    public string? WhitelistedReason { get; set; }
    public string? WhitelistedBy { get; set; }
}
