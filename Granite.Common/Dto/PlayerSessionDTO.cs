using System;
using Sieve.Attributes;

namespace Granite.Common.Dto;

public class PlayerSessionDTO
{
    [Sieve(CanFilter = true, CanSort = true)]
    public Guid Id { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string PlayerId { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public Guid ServerId { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string ServerName { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime JoinDate { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? LeaveDate { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public string IpAddress { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public string PlayerName { get; set; } = string.Empty;

    [Sieve(CanFilter = true, CanSort = true)]
    public double? Duration { get; set; }

    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsActive { get; set; }
}
