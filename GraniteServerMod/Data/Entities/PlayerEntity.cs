using System;

namespace GraniteServerMod.Data.Entities;

public class PlayerEntity
{
    public string Id { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; }
    public DateTime FirstJoinDate { get; set; }
    public DateTime LastJoinDate { get; set; }
}
