namespace GraniteServer.Api.Models;

public class ModDTO
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string? CurrentVersion { get; internal set; }
    public string InstalledVersion { get; internal set; }
}
