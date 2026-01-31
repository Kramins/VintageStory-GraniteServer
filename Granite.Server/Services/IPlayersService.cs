using Granite.Common.Dto;

namespace Granite.Server.Services;

public interface IPlayersService
{
    Task<PlayerNameIdDTO?> FindPlayerByNameAsync(string name, CancellationToken cancellationToken = default);
}
