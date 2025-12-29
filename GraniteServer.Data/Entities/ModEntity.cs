namespace GraniteServer.Data.Entities;

public class ModEntity
{
    public long ModId { get; set; } // Primary Key
    public string ModIdStr { get; set; } = string.Empty; // Unique Mod Identifier, indexed
    public string Name { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Author { get; set; }
    public string? UrlAlias { get; set; }
    public string? LogoFilename { get; set; }
    public string? LogoFile { get; set; }
    public string? LogoFileDb { get; set; }
    public string? HomePageUrl { get; set; }
    public string? SourceCodeUrl { get; set; }
    public string? TrailerVideoUrl { get; set; }
    public string? IssueTrackerUrl { get; set; }
    public string? WikiUrl { get; set; }
    public int Downloads { get; set; }
    public int Follows { get; set; }
    public int TrendingPoints { get; set; }
    public int Comments { get; set; }
    public string? Side { get; set; }
    public string? Type { get; set; }
    public string? Created { get; set; }
    public string? LastReleased { get; set; }
    public string? LastModified { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime LastChecked { get; set; }
}
