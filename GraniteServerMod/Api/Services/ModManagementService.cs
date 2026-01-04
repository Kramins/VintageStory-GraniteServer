using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GraniteServer;
using GraniteServer.Api.Models;
using GraniteServer.Api.Models.ModDatabase;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class ModManagementService
{
    public TimeSpan ModDataCacheDuration { get; } = TimeSpan.FromSeconds(300);

    public string ModDownloadFolderPath => Path.Combine(_api.DataBasePath, "downloads");
    public string ModInstallFolderPath => Path.Combine(_api.DataBasePath, "mods");
    private const string ModApiBaseAddress = "https://mods.vintagestory.at/api/";
    private const string UserAgent = "GraniteServer/1.0";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly ICoreServerAPI _api;
    private readonly ILogger _logger;
    private readonly GraniteDataContext _dataContext;
    private readonly GraniteServerConfig _config;

    public ModManagementService(
        ICoreServerAPI api,
        GraniteServerConfig config,
        GraniteDataContext dataContext,
        ILogger logger
    )
    {
        _api = api;
        _logger = logger;
        _config = config;
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

    private ModDatabaseEntry RetrieveModDataFromVintageStoryRemoteStore(string modId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"mod/{modId}");
        var response = HttpClient.Send(request);

        ModDatabaseResponse? payload;
        try
        {
            payload = response.Content.ReadFromJsonAsync<ModDatabaseResponse>().Result;
        }
        catch
        {
            payload = null;
        }

        return payload?.Mod ?? throw new Exception("Failed to retrieve mod data from API.");
    }

    private async Task<ModEntity> GetAndUpdateModData(string modIdStr)
    {
        var modEntity = _dataContext.Mods.FirstOrDefault(m => m.ModIdStr == modIdStr);
        if (modEntity == null || modEntity.LastChecked.Add(ModDataCacheDuration) < DateTime.UtcNow)
        {
            var modData = RetrieveModDataFromVintageStoryRemoteStore(modIdStr);
            modEntity = UpsertModWithReleases(modData, modIdStr);
        }
        return modEntity;
    }

    public List<ModDTO> GetServerMods()
    {
        var mods = _dataContext
            .ModServers.Include(ms => ms.Mod)
                .ThenInclude(m => m.Releases)
            .Where(ms => ms.ServerId == _config.ServerId)
            .ToList();

        var modDtos = mods.Select(ms => new ModDTO
            {
                ModId = ms.ModId,
                Name = ms.Mod.Name,
                Author = ms.Mod.Author,
                RunningVersion = ms
                    .Mod.Releases.FirstOrDefault(r => r.Id == ms.RunningReleaseId)
                    ?.ModVersion,
                InstalledVersion = ms
                    .Mod.Releases.FirstOrDefault(r => r.Id == ms.InstalledReleaseId)
                    ?.ModVersion,
            })
            .ToList();

        return modDtos;
    }

    public async Task<string> InstallOrUpdateModAsync(string modIdStr)
    {
        var modData = await GetAndUpdateModData(modIdStr);
        var installedMod = _api.ModLoader.GetMod(modIdStr);
        var modRelease = modData.Releases.OrderByDescending(r => r.Created).LastOrDefault();

        if (installedMod != null && installedMod.Info.Version == modRelease?.ModVersion)
        {
            throw new Exception("Mod is already installed with the latest version.");
        }

        if (installedMod != null)
        {
            var installedFilePath = Path.Combine(ModInstallFolderPath, installedMod.FileName);
            File.Delete(installedFilePath);
        }

        var downloadedFilePath = DownloadModReleaseFile(modRelease);
        var destPath = Path.Combine(ModInstallFolderPath, modRelease.Filename);

        File.Move(downloadedFilePath, destPath);

        return "";
    }

    public async Task SyncModsAsync(CancellationToken token)
    {
        _logger.Notification("Starting mod synchronization...");
        var runningMods = _api.ModLoader.Mods.Where(mod => mod.Info.CoreMod == false).ToList();
        _logger.Notification($"Found {runningMods.Count} running mods.");
        var modServerItems = _dataContext
            .ModServers.Where(ms => ms.ServerId == _config.ServerId)
            .ToList();

        // Update or insert mods
        foreach (var mod in runningMods)
        {
            _logger.Notification($"Processing mod: {mod.Info.ModID}");
            var modEntity = await GetAndUpdateModData(mod.Info.ModID);

            var RunningReleaseId = modEntity
                .Releases.FirstOrDefault(r => r.ModVersion == mod.Info.Version)
                ?.Id;

            var modServerEntity = modServerItems.FirstOrDefault(ms => ms.ModId == modEntity.Id);

            if (modServerEntity == null)
            {
                _logger.Notification($"Adding new mod server entity for mod: {mod.Info.ModID}");
                modServerEntity = new ModServerEntity
                {
                    ServerId = _config.ServerId,
                    ModId = modEntity.Id,
                    RunningReleaseId = RunningReleaseId,
                    InstalledReleaseId = RunningReleaseId,
                };
                _dataContext.ModServers.Add(modServerEntity);
            }
            else
            {
                _logger.Notification(
                    $"Updating existing mod server entity for mod: {mod.Info.ModID}"
                );
                modServerEntity.RunningReleaseId = RunningReleaseId;
                modServerEntity.InstalledReleaseId = RunningReleaseId;
            }
            _dataContext.SaveChanges();
            _logger.Notification($"Finished processing mod: {mod.Info.ModID}");
        }
        _logger.Notification("Mod synchronization completed.");
    }

    private string DownloadModReleaseFile(ModReleaseEntity modReleaseEntity)
    {
        var downloadUrl = modReleaseEntity.MainFile;
        var fileName = modReleaseEntity.Filename;

        if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
        {
            throw new Exception("Mod release does not have a valid download URL or filename.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        var response = HttpClient.Send(request);

        Directory.CreateDirectory(ModDownloadFolderPath);
        var filePath = Path.Combine(ModDownloadFolderPath, fileName);

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            response.Content.CopyToAsync(fs).Wait();
        }

        return filePath;
    }

    public ModEntity UpsertModWithReleases(ModDatabaseEntry mod, string ModIdStr)
    {
        // Upsert ModEntity by ModId
        var existingMod = _dataContext
            .Mods.Include(m => m.Releases)
            .FirstOrDefault(m => m.ModId == mod.ModId);

        if (existingMod == null)
        {
            var modEntity = MapModEntryToEntity(mod, ModIdStr);

            foreach (var release in mod.Releases)
            {
                var modReleaseEntity = MapModReleaseEntryToEntity(release);
                modEntity.Releases.Add(modReleaseEntity);
            }

            _dataContext.Mods.Add(modEntity);

            existingMod = modEntity;
        }
        else
        {
            MapModEntryToEntity(mod, ModIdStr, existingMod);

            // Upsert ModReleaseEntities
            foreach (var release in mod.Releases)
            {
                var existingRelease = existingMod.Releases.FirstOrDefault(r =>
                    r.ReleaseId == release.ReleaseId
                );
                if (existingRelease == null)
                {
                    var modReleaseEntity = MapModReleaseEntryToEntity(release);
                    existingMod.Releases.Add(modReleaseEntity);
                }
                else
                {
                    MapModReleaseEntryToEntity(release, existingRelease);
                }
            }
        }

        _dataContext.SaveChanges();
        return existingMod;
    }

    private ModEntity MapModEntryToEntity(ModDatabaseEntry modEntry, string ModIdStr)
    {
        return MapModEntryToEntity(modEntry, ModIdStr, new ModEntity());
    }

    private ModEntity MapModEntryToEntity(
        ModDatabaseEntry modEntry,
        string ModIdStr,
        ModEntity existingEntity
    )
    {
        existingEntity.ModId = modEntry.ModId;
        existingEntity.ModIdStr = ModIdStr;
        existingEntity.Name = modEntry.Name ?? "Unknown"; // Added null check to prevent null reference assignment
        existingEntity.Author = modEntry.Author;
        existingEntity.UrlAlias = modEntry.UrlAlias;
        existingEntity.Side = modEntry.Side;
        existingEntity.Type = modEntry.Type;
        existingEntity.Tags = modEntry.Tags ?? new List<string>();
        existingEntity.LastChecked = DateTime.UtcNow;
        return existingEntity;
    }

    private ModReleaseEntity MapModReleaseEntryToEntity(ModRelease releaseEntry)
    {
        return MapModReleaseEntryToEntity(releaseEntry, new ModReleaseEntity());
    }

    private ModReleaseEntity MapModReleaseEntryToEntity(
        ModRelease releaseEntry,
        ModReleaseEntity existingEntity
    )
    {
        existingEntity.ReleaseId = releaseEntry.ReleaseId ?? 0; // Added null coalescing operator to handle nullable value type
        existingEntity.MainFile = releaseEntry.MainFile;
        existingEntity.Filename = releaseEntry.Filename;
        existingEntity.FileId = releaseEntry.FileId;
        existingEntity.Downloads = releaseEntry.Downloads;
        existingEntity.Tags = releaseEntry.Tags ?? new List<string>();
        existingEntity.ModIdStr = releaseEntry.ModIdStr;
        existingEntity.ModVersion = releaseEntry.ModVersion;
        existingEntity.Created = releaseEntry.Created;
        existingEntity.Changelog = releaseEntry.Changelog;
        return existingEntity;
    }
}
