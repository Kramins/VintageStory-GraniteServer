using System;
using Sieve.Attributes;

namespace Granite.Common.Dto;

public record PlayerSessionDTO
{
    [Sieve(CanFilter = true, CanSort = true)]
    public Guid Id { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string PlayerId { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public Guid ServerId { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string ServerName { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime JoinDate { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? LeaveDate { get; init; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string IpAddress { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string PlayerName { get; init; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public double? Duration { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsActive { get; set; }
}
