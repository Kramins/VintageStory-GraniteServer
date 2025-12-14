using System;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Models;

public class PlayerDTO
{
    public string Name { get; internal set; }
    public string Id { get; internal set; }
    public bool IsAdmin { get; internal set; }
    public string IpAddress { get; internal set; }
    public string LanguageCode { get; internal set; }
    public float Ping { get; internal set; }
    public string RolesCode { get; internal set; }
    public string FirstJoinDate { get; internal set; }
    public string LastJoinDate { get; internal set; }
    public string[] Privileges { get; internal set; }
    public string ConnectionState { get; internal set; }
    public bool IsBanned { get; internal set; }
    public bool IsWhitelisted { get; internal set; }
    public string? BanReason { get; internal set; }
    public string? BanBy { get; internal set; }
    public DateTime? BanUntil { get; internal set; }
    public string? WhitelistedReason { get; internal set; }
    public string? WhitelistedBy { get; internal set; }
    public DateTime? WhitelistedUntil { get; internal set; }
}

