import axios from 'axios';
import type { PlayerDTO } from '../types/PlayerDTO';
import type { PlayerDetailsDTO } from '../types/PlayerDetailsDTO';
import type { UpdateInventorySlotRequestDTO } from '../types/UpdateInventorySlotRequestDTO';
import type { PlayerNameIdDTO } from '../types/PlayerNameIdDTO';
import type { PlayerSessionDTO } from '../types/PlayerSessionDTO';
import type { JsonApiDocument, PaginationMeta, JsonApiError } from '../types/JsonApi';

const API_BASE = '/api/players';

export const PlayerService = {
    async getAllPlayers(): Promise<PlayerDTO[]> {
        const response = await axios.get<JsonApiDocument<PlayerDTO[]>>(`${API_BASE}/`);
        const document = response.data;
        return document?.data ?? [];
    },

    async getAllPlayersPaged(
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

        const response = await axios.get<JsonApiDocument<PlayerDTO[]>>(`${API_BASE}/`, {
            params: { page: apiPage, pageSize: apiPageSize, sorts, filters },
        });

        const document = response.data;
        const players = document?.data ?? [];
        const pagination = document?.meta?.pagination;
        const errors = document?.errors ?? [];
        return { players, pagination, errors };
    },

    async getOnlinePlayers(
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add ConnectionState=='Connected' to the filter
        const onlineFilter = filters ? `ConnectionState=='Connected'&&${filters}` : `ConnectionState=='Connected'`;
        return this.getAllPlayersPaged(page, pageSize, sortField, sortDirection, onlineFilter);
    },

    async getWhitelistedPlayers(
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add IsWhitelisted==true to the filter
        const whitelistFilter = filters ? `IsWhitelisted==true&&${filters}` : 'IsWhitelisted==true';
        return this.getAllPlayersPaged(page, pageSize, sortField, sortDirection, whitelistFilter);
    },

    async getBannedPlayers(
        page = 0,
        pageSize = 20,
        sortField = 'id',
        sortDirection: 'asc' | 'desc' = 'asc',
        filters = ''
    ): Promise<{ players: PlayerDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        // Add IsBanned==true to the filter
        const bannedFilter = filters ? `IsBanned==true&&${filters}` : 'IsBanned==true';
        return this.getAllPlayersPaged(page, pageSize, sortField, sortDirection, bannedFilter);
    },

    async kickPlayer(playerId: string, reason?: string): Promise<void> {
        await axios.post(`${API_BASE}/id/${playerId}/kick`, { reason });
    },

    async banPlayer(playerId: string, reason?: string): Promise<void> {
        await axios.post(`${API_BASE}/id/${playerId}/ban`, { reason });
    },

    async unBanPlayer(playerId: string): Promise<void> {
        await axios.delete(`${API_BASE}/id/${playerId}/ban`);
    },

    async whitelistPlayer(playerId: string): Promise<void> {
        await axios.post(`${API_BASE}/id/${playerId}/whitelist`);
    },
    
    async unWhitelistPlayer(playerId: string): Promise<void> {
        await axios.delete(`${API_BASE}/id/${playerId}/whitelist`);
    },

    async getPlayerDetails(playerId: string): Promise<PlayerDetailsDTO> {
        const response = await axios.get(`${API_BASE}/id/${playerId}`);
        return response.data;
    },
    async updatePlayerInventorySlot(playerId: string, inventoryName: string, data: UpdateInventorySlotRequestDTO): Promise<void> {
        await axios.post(`${API_BASE}/id/${playerId}/inventories/${inventoryName}/`, data);
    },
    async removeItemFromInventory(playerId: string, inventoryName: string, slotIndex: number): Promise<void> {
        await axios.delete(`${API_BASE}/id/${playerId}/inventories/${inventoryName}/${slotIndex}`);
    },

    async findPlayerByName(name: string): Promise<PlayerNameIdDTO> {
        const response = await axios.get(`${API_BASE}/find`, { params: { name } });
        return response.data;
    },

    async getPlayerSessions(
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
            `${API_BASE}/id/${playerId}/sessions`,
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
