using System;

namespace GraniteServer.Common;

public class PlayerSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? RoleCode { get; set; }

    public bool IsOnline { get; set; }
}

public class DetailedPlayerSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public float Ping { get; set; } = 0;

    public string RolesCode { get; set; } = string.Empty;

    public string FirstJoinDate { get; set; } = string.Empty;

    public string LastJoinDate { get; set; } = string.Empty;

    public string[] Privileges { get; set; } = Array.Empty<string>();

    public string ConnectionState { get; set; } = string.Empty;

    public bool IsBanned { get; set; }

    public bool IsWhitelisted { get; set; }

    public string? BanReason { get; set; }

    public string? BanBy { get; set; }

    public DateTime? BanUntil { get; set; }

    public string? WhitelistedReason { get; set; }

    public string? WhitelistedBy { get; set; }

    public DateTime? WhitelistedUntil { get; set; }
}
