using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GraniteServer.Data.Entities;
using GraniteServerMod.Api.Models;
using GraniteServerMod.Api.Models.ModDatabase;
using GraniteServerMod.Data;
using Microsoft.EntityFrameworkCore;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServerMod.Api.Services;

public class ModManagementService
{
    public TimeSpan ModDataCacheDuration { get; } = TimeSpan.FromHours(1);
    private const string ModApiBaseAddress = "https://mods.vintagestory.at/api/";
    private const string UserAgent = "GraniteServerMod/1.0 (+github:GraniteServer)";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly ICoreServerAPI _api;
    private readonly GraniteDataContext _dataContext;

    public ModManagementService(ICoreServerAPI api, GraniteDataContext dataContext)
    {
        _api = api;
        _dataContext = dataContext;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(ModApiBaseAddress),
            Timeout = TimeSpan.FromSeconds(15),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private async Task<ModDatabaseEntry> RetrieveModAsync(string modId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"mod/{modId}");
        var response = await HttpClient.SendAsync(request);

        ModDatabaseResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<ModDatabaseResponse>();
        }
        catch
        {
            payload = null;
        }

        return payload?.Mod ?? throw new Exception("Failed to retrieve mod data from API.");
    }

    private async Task<ModEntity> GetModAsync(string modIdStr)
    {
        var modEntity = await _dataContext.Mods.FirstOrDefaultAsync(m => m.ModIdStr == modIdStr);
        if (modEntity == null)
        {
            var modData = await RetrieveModAsync(modIdStr);
            modEntity = MapModDatabaseEntryToModEntity(modIdStr, modData, modEntity);
            modEntity.LastChecked = DateTime.UtcNow;
            _dataContext.Mods.Update(modEntity);
            await _dataContext.SaveChangesAsync();
            return modEntity;
        }

        if (modEntity.LastChecked.Add(ModDataCacheDuration) < DateTime.UtcNow)
        {
            var modData = await RetrieveModAsync(modIdStr);
            modEntity = MapModDatabaseEntryToModEntity(modIdStr, modData, modEntity);
            modEntity.LastChecked = DateTime.UtcNow;
            _dataContext.Mods.Update(modEntity);
            await _dataContext.SaveChangesAsync();
        }
        return modEntity;
    }

    private ModEntity MapModDatabaseEntryToModEntity(
        string modStr,
        ModDatabaseEntry modData,
        ModEntity? modEntity
    )
    {
        var entity = modEntity ?? new ModEntity();
        entity.ModId = modData.ModId;
        entity.ModIdStr = modStr;
        entity.Name = modData.Name ?? string.Empty;
        entity.Text = modData.Text;
        entity.Author = modData.Author;
        entity.UrlAlias = modData.UrlAlias;
        entity.LogoFilename = modData.LogoFilename;
        entity.LogoFile = modData.LogoFile;
        entity.LogoFileDb = modData.LogoFileDb;
        entity.HomePageUrl = modData.HomePageUrl;
        entity.SourceCodeUrl = modData.SourceCodeUrl;
        entity.TrailerVideoUrl = modData.TrailerVideoUrl;
        entity.IssueTrackerUrl = modData.IssueTrackerUrl;
        entity.WikiUrl = modData.WikiUrl;
        entity.Downloads = modData.Downloads;
        entity.Follows = modData.Follows;
        entity.TrendingPoints = modData.TrendingPoints;
        entity.Comments = modData.Comments;
        entity.Side = modData.Side;
        entity.Type = modData.Type;
        entity.Created = modData.Created;
        entity.LastReleased = modData.LastReleased;
        entity.LastModified = modData.LastModified;
        entity.Tags = modData.Tags ?? new List<string>();

        // Map releases
        if (modData.Releases != null && modData.Releases.Count > 0)
        {
            // Remove existing releases for this mod if needed, then add new ones
            var existingReleases = _dataContext
                .ModReleases.Where(r => r.ModId == entity.ModId)
                .ToList();
            _dataContext.ModReleases.RemoveRange(existingReleases);
            var newReleases = modData
                .Releases.Select(r => MapModReleaseToEntity(r, entity.ModId))
                .ToList();
            _dataContext.ModReleases.AddRange(newReleases);
        }

        return entity;
    }

    private ModReleaseEntity MapModReleaseToEntity(ModRelease release, long modId)
    {
        return new ModReleaseEntity
        {
            ModId = modId,
            ReleaseId = release.ReleaseId.HasValue
                ? release.ReleaseId.Value
                : throw new Exception("ReleaseId is required."),
            MainFile = release.MainFile,
            Filename = release.Filename,
            FileId = release.FileId,
            Downloads = release.Downloads,
            Tags = release.Tags ?? new List<string>(),
            ModIdStr = release.ModIdStr,
            ModVersion = release.ModVersion,
            Created = release.Created,
            Changelog = release.Changelog,
        };
    }

    public async Task<List<ModDTO>> GetInstalledModsAsync()
    {
        // Placeholder implementation

        var mods = _api.ModLoader.Mods.Where(mod => mod.Info.CoreMod == false);
        var modDtoList = new List<ModDTO>();
        foreach (var mod in mods)
        {
            var modData = await GetModAsync(mod.Info.ModID);

            var modDto = MapModEntityToDto(mod, modData);
            modDtoList.Add(modDto);
        }
        return modDtoList;
    }

    private ModDTO MapModEntityToDto(Mod mod, ModEntity modData)
    {
        return new ModDTO
        {
            Id = mod.Info.ModID,
            CurrentVersion = modData
                .Releases.OrderByDescending(r => r.Created)
                .LastOrDefault()
                ?.ModVersion,
            InstalledVersion = mod.Info.Version,
            Name = mod.Info.Name,
        };
    }
}
