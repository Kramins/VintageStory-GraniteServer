using System;
using Sieve.Attributes;

namespace Granite.Common.Dto;

public class PlayerDTO
{
    [Sieve(CanFilter = true, CanSort = true)]
    public string Id { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string Name { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsAdmin { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string IpAddress { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string LanguageCode { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public float Ping { get; set; } = 0;

    [Sieve(CanFilter = true, CanSort = true)]
    public string RolesCode { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string FirstJoinDate { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string LastJoinDate { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string[] Privileges { get; set; } = Array.Empty<string>();

    [Sieve(CanFilter = true, CanSort = true)]
    public string ConnectionState { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsBanned { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsWhitelisted { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? BanReason { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? BanBy { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? BanUntil { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? WhitelistedReason { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? WhitelistedBy { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? WhitelistedUntil { get; set; }
}
