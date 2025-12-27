using System;

namespace GraniteServerMod.Data.Entities;

public class PlayerEntity
{
    public string Id { get; internal set; }
    public string Name { get; internal set; }
    public DateTime FirstJoinDate { get; internal set; }
    public DateTime LastJoinDate { get; internal set; }
    public Guid ServerId { get; internal set; }
}
