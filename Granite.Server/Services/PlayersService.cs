using Granite.Common.Dto;
using Granite.Common.Services;
using GraniteServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Granite.Server.Services;

/// <summary>
/// Global player service that searches across all servers and caches external API lookups
/// </summary>
public class PlayersService : IPlayersService
{
    private readonly ILogger<PlayersService> _logger;
    private readonly GraniteDataContext _dbContext;
    private readonly IPlayerNameResolver _playerNameResolver;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public PlayersService(
        ILogger<PlayersService> logger,
        GraniteDataContext dbContext,
        IPlayerNameResolver playerNameResolver,
        IMemoryCache cache)
    {
        _logger = logger;
        _dbContext = dbContext;
        _playerNameResolver = playerNameResolver;
        _cache = cache;
    }

    /// <summary>
    /// Finds a player by name across all servers. Searches the database first,
    /// then falls back to the Vintage Story API if not found. Caches API results.
    /// </summary>
    /// <param name="name">The player name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Player name and ID if found, null otherwise</returns>
    public virtual async Task<PlayerNameIdDTO?> FindPlayerByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var cacheKey = $"player:name:{name.ToLowerInvariant()}";

        // Check cache first
        if (_cache.TryGetValue<PlayerNameIdDTO>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Player name lookup cache hit for '{Name}'", name);
            return cachedResult;
        }

        // Search database across all servers, ordered by most recent activity
        var playerFromDb = await _dbContext.Players
            .Where(p => p.Name == name)
            .OrderByDescending(p => p.LastJoinDate)
            .Select(p => new PlayerNameIdDTO
            {
                Id = p.PlayerUID,
                Name = p.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (playerFromDb != null)
        {
            _logger.LogDebug("Player '{Name}' found in database with UID '{PlayerUID}'", name, playerFromDb.Id);
            
            // Cache the database result
            _cache.Set(cacheKey, playerFromDb, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheExpiration
            });
            
            return playerFromDb;
        }

        // Not in database, try external Vintage Story API
        _logger.LogDebug("Player '{Name}' not found in database, querying Vintage Story API", name);
        
        var playerUid = await _playerNameResolver.ResolvePlayerNameAsync(name, cancellationToken);
        
        if (playerUid == null)
        {
            _logger.LogDebug("Player '{Name}' not found in Vintage Story API", name);
            return null;
        }

        var result = new PlayerNameIdDTO
        {
            Id = playerUid,
            Name = name
        };

        // Cache the API result with sliding expiration
        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheExpiration
        });

        _logger.LogInformation("Player '{Name}' resolved from Vintage Story API with UID '{PlayerUID}' and cached", name, playerUid);
        
        return result;
    }
}
