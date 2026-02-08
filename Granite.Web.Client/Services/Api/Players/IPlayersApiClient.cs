using System;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api.Players;

public interface IPlayersApiClient
{
    Task<JsonApiDocument<PlayerNameIdDTO>> FindPlayerByNameAsync(
        string playerName
    );
}
