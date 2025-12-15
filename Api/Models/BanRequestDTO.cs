using System;

namespace GraniteServer.Api.Models;

public class BanRequestDTO
{
    public string? IssuedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime? UntilDate { get; set; }
}
