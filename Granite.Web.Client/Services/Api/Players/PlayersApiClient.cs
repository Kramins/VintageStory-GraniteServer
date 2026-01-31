using System;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api.Players;

public class PlayersApiClient : BaseApiClient, IPlayersApiClient
{
    private string GetBasePath() => $"/api/players";

    public PlayersApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ServerPlayersApiClient> logger
    )
        : base(httpClientFactory, logger) { }

    public async Task<JsonApiDocument<List<PlayerNameIdDTO>>> FindPlayerByNameAsync(
        string playerName
    )
    {
        try
        {
            return await GetAsync<JsonApiDocument<List<PlayerNameIdDTO>>>(
                $"{GetBasePath()}/find?name={Uri.EscapeDataString(playerName)}"
            );
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to find player by name {PlayerName}", playerName);
            throw;
        }
    }
}
