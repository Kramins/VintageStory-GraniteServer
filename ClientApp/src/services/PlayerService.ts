import axios from 'axios';
import type { PlayerDTO } from '../types/PlayerDTO';
import type { PlayerDetailsDTO } from '../types/PlayerDetailsDTO';

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
        await axios.post(`${API_BASE}/${playerId}/kick`, { reason });
    },

    async banPlayer(playerId: string, reason?: string): Promise<void> {
        await axios.post(`${API_BASE}/${playerId}/ban`, { reason });
    },

    async unBanPlayer(playerId: string): Promise<void> {
        await axios.delete(`${API_BASE}/${playerId}/ban`);
    },

    async whitelistPlayer(playerId: string): Promise<void> {
        await axios.post(`${API_BASE}/${playerId}/whitelist`);
    },
    
    async unWhitelistPlayer(playerId: string): Promise<void> {
        await axios.delete(`${API_BASE}/${playerId}/whitelist`);
    },

    async getPlayerDetails(playerId: string): Promise<PlayerDetailsDTO> {
        const response = await axios.get(`${API_BASE}/${playerId}`);
        return response.data;
    }


    // Add more methods as needed based on backend endpoints
};
