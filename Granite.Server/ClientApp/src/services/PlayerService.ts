import axios from 'axios';
import type { PlayerDTO } from '../types/PlayerDTO';
import type { PlayerDetailsDTO } from '../types/PlayerDetailsDTO';
import type { UpdateInventorySlotRequestDTO } from '../types/UpdateInventorySlotRequestDTO';
import type { PlayerNameIdDTO } from '../types/PlayerNameIdDTO';
import type { PlayerSessionDTO } from '../types/PlayerSessionDTO';
import type { JsonApiDocument, PaginationMeta, JsonApiError } from '../types/JsonApi';

const getApiBase = (serverId: string) => `/api/${serverId}/players`;

export const PlayerService = {
    async getAllPlayers(serverId: string): Promise<PlayerDTO[]> {
        const response = await axios.get<JsonApiDocument<PlayerDTO[]>>(`${getApiBase(serverId)}/`);
        const document = response.data;
        return document?.data ?? [];
    },

    async getAllPlayersPaged(
        serverId: string,
        page = 1,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        const sorts = sortDirection === 'desc' ? `-${sortField}` : sortField;

        // API expects 1-based page
        const apiPage = page < 1 ? 1 : page;
        const apiPageSize = pageSize <= 0 ? 1 : pageSize;

        const response = await axios.get<JsonApiDocument<PlayerDTO[]>>(`${getApiBase(serverId)}/`, {
            params: { page: apiPage, pageSize: apiPageSize, sorts, filters },
        });

        const document = response.data;
        const players = document?.data ?? [];
        const pagination = document?.meta?.pagination;
        const errors = document?.errors ?? [];
        return { players, pagination, errors };
    },

    async getOnlinePlayers(
        serverId: string,
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add ConnectionState=='Connected' to the filter
        const onlineFilter = filters ? `ConnectionState=='Connected'&&${filters}` : `ConnectionState=='Connected'`;
        return this.getAllPlayersPaged(serverId, page, pageSize, sortField, sortDirection, onlineFilter);
    },

    async getWhitelistedPlayers(
        serverId: string,
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add IsWhitelisted==true to the filter
        const whitelistFilter = filters ? `IsWhitelisted==true&&${filters}` : 'IsWhitelisted==true';
        return this.getAllPlayersPaged(serverId, page, pageSize, sortField, sortDirection, whitelistFilter);
    },

    async getBannedPlayers(
        serverId: string,
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add IsBanned==true to the filter
        const bannedFilter = filters ? `IsBanned==true&&${filters}` : 'IsBanned==true';
        return this.getAllPlayersPaged(serverId, page, pageSize, sortField, sortDirection, bannedFilter);
    },

    async kickPlayer(serverId: string, playerId: string, reason?: string): Promise<void> {
        await axios.post(`${getApiBase(serverId)}/${playerId}/kick`, { reason });
    },

    async banPlayer(serverId: string, playerId: string, reason?: string): Promise<void> {
        await axios.post(`${getApiBase(serverId)}/${playerId}/ban`, { reason });
    },

    async unBanPlayer(serverId: string, playerId: string): Promise<void> {
        await axios.delete(`${getApiBase(serverId)}/${playerId}/ban`);
    },

    async whitelistPlayer(serverId: string, playerId: string): Promise<void> {
        await axios.post(`${getApiBase(serverId)}/${playerId}/whitelist`);
    },
    
    async unWhitelistPlayer(serverId: string, playerId: string): Promise<void> {
        await axios.delete(`${getApiBase(serverId)}/${playerId}/whitelist`);
    },

    async getPlayerDetails(serverId: string, playerId: string): Promise<PlayerDetailsDTO> {
        const response = await axios.get<JsonApiDocument<PlayerDetailsDTO>>(`${getApiBase(serverId)}/${playerId}`);
        return response.data.data;
    },
    async updatePlayerInventorySlot(serverId: string, playerId: string, slotIndex: number, data: UpdateInventorySlotRequestDTO): Promise<void> {
        await axios.post(`${getApiBase(serverId)}/${playerId}/inventory/${slotIndex}`, data);
    },
    async removeItemFromInventory(serverId: string, playerId: string, slotIndex: number): Promise<void> {
        await axios.delete(`${getApiBase(serverId)}/${playerId}/inventory/${slotIndex}`);
    },

    async findPlayerByName(serverId: string, name: string): Promise<PlayerNameIdDTO> {
        const response = await axios.get(`${getApiBase(serverId)}/find`, { params: { name } });
        return response.data;
    },

    async getPlayerSessions(
        serverId: string,
        playerId: string,
        page = 1,
        pageSize = 20,
        sortField = 'joinDate',
        sortDirection: 'asc' | 'desc' = 'desc',
        filters = ''
    ): Promise<{ sessions: PlayerSessionDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        const sorts = sortDirection === 'desc' ? `-${sortField}` : sortField;

        const apiPage = page < 1 ? 1 : page;
        const apiPageSize = pageSize <= 0 ? 1 : pageSize;

        const response = await axios.get<JsonApiDocument<PlayerSessionDTO[]>>(
            `${getApiBase(serverId)}/${playerId}/sessions`,
            { params: { page: apiPage, pageSize: apiPageSize, sorts, filters } }
        );

        const document = response.data;
        const sessions = document?.data ?? [];
        const pagination = document?.meta?.pagination;
        const errors = document?.errors ?? [];

        return { sessions, pagination, errors };
    }


    // Add more methods as needed based on backend endpoints
};
