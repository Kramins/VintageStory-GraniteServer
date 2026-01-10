using System;

namespace GraniteServer.Api.Messaging.Contracts;

public class PlayerBanEventData : PlayerEventData
{
    public string Reason { get; set; }
    public string IssuedBy { get; set; }
    public DateTime? UntilDate { get; set; }
}
