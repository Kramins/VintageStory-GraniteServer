using System;

namespace Granite.Common.Dto;

public class BanRequestDTO
{
    public string? IssuedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime? UntilDate { get; set; }
}
