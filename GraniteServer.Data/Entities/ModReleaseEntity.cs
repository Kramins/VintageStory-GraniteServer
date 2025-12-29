namespace GraniteServer.Data.Entities;

public class ModReleaseEntity
{
    public long ReleaseId { get; set; } // Primary Key
    public long ModId { get; set; } // Foreign Key to ModEntity
    public string? MainFile { get; set; }
    public string? Filename { get; set; }
    public long? FileId { get; set; }
    public int Downloads { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ModIdStr { get; set; }
    public string? ModVersion { get; set; }
    public string? Created { get; set; }
    public string? Changelog { get; set; }
}
