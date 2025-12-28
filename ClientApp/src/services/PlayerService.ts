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
        const response = await axios.get(`${API_BASE}/`);
        return response.data;
    },

    async getWhitelistedPlayers(): Promise<PlayerDTO[]> {
        const response = await axios.get(`${API_BASE}/whitelisted`);
        return response.data;
    },

    async getBannedPlayers(): Promise<PlayerDTO[]> {
        const response = await axios.get(`${API_BASE}/banned`);
        return response.data;
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
        page = 0,
        pageSize = 20
    ): Promise<{ sessions: PlayerSessionDTO[]; pagination?: PaginationMeta; errors?: JsonApiError[] }> {
        const response = await axios.get<JsonApiDocument<PlayerSessionDTO[]>>(
            `${API_BASE}/id/${playerId}/sessions`,
            { params: { page, pageSize } }
        );

        const document = response.data;
        const sessions = document?.data ?? [];
        const pagination = document?.meta?.pagination;
        const errors = document?.errors ?? [];

        return { sessions, pagination, errors };
    }


    // Add more methods as needed based on backend endpoints
};
