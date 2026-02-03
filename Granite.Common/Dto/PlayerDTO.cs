using System;
using Sieve.Attributes;

namespace Granite.Common.Dto;

public record PlayerDTO
{
    [Sieve(CanFilter = true, CanSort = true)]
    public Guid Id { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string PlayerUID { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public Guid ServerId { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string Name { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsAdmin { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string IpAddress { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string LanguageCode { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public float Ping { get; init; } = 0;

    [Sieve(CanFilter = true, CanSort = true)]
    public string RolesCode { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string FirstJoinDate { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string LastJoinDate { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string[] Privileges { get; init; } = Array.Empty<string>();

    [Sieve(CanFilter = true, CanSort = true)]
    public string ConnectionState { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsBanned { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsWhitelisted { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? BanReason { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? BanBy { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? BanUntil { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? WhitelistedReason { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string? WhitelistedBy { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? WhitelistedUntil { get; init; }
}
