using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraniteServerMod.Api.Models.ModDatabase;

public class ModDatabaseResponse
{
    [JsonPropertyName("mod")]
    public ModDatabaseEntry? Mod { get; set; }

    [JsonPropertyName("statuscode")]
    public string? StatusCode { get; set; }
}

public class ModDatabaseEntry
{
    [JsonPropertyName("modid")]
    public long ModId { get; set; }

    [JsonPropertyName("assetid")]
    public long AssetId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("urlalias")]
    public string? UrlAlias { get; set; }

    [JsonPropertyName("logofilename")]
    public string? LogoFilename { get; set; }

    [JsonPropertyName("logofile")]
    public string? LogoFile { get; set; }

    [JsonPropertyName("logofiledb")]
    public string? LogoFileDb { get; set; }

    [JsonPropertyName("homepageurl")]
    public string? HomePageUrl { get; set; }

    [JsonPropertyName("sourcecodeurl")]
    public string? SourceCodeUrl { get; set; }

    [JsonPropertyName("trailervideourl")]
    public string? TrailerVideoUrl { get; set; }

    [JsonPropertyName("issuetrackerurl")]
    public string? IssueTrackerUrl { get; set; }

    [JsonPropertyName("wikiurl")]
    public string? WikiUrl { get; set; }

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("follows")]
    public int Follows { get; set; }

    [JsonPropertyName("trendingpoints")]
    public int TrendingPoints { get; set; }

    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("lastreleased")]
    public string? LastReleased { get; set; }

    [JsonPropertyName("lastmodified")]
    public string? LastModified { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("releases")]
    public List<ModRelease> Releases { get; set; } = new();

    [JsonPropertyName("screenshots")]
    public List<ModScreenshot> Screenshots { get; set; } = new();
}

public class ModRelease
{
    [JsonPropertyName("releaseid")]
    public long? ReleaseId { get; set; }

    [JsonPropertyName("mainfile")]
    public string? MainFile { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("fileid")]
    public long? FileId { get; set; }

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("modidstr")]
    public string? ModIdStr { get; set; }

    [JsonPropertyName("modversion")]
    public string? ModVersion { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }
}

public class ModScreenshot
{
    // Add properties if needed based on actual API output
}
