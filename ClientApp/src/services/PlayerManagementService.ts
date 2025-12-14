import axios from 'axios';
import type { PlayerDTO } from '../types/PlayerDTO';

const API_BASE = '/api/players';

export const PlayerManagementService = {
    async getPlayers(): Promise<PlayerDTO[]> {
        const response = await axios.get(`${API_BASE}/list-players/`);
        return response.data;
    },


    async kickPlayer(playerId: string, reason?: string): Promise<void> {
        await axios.post(`${API_BASE}/kick/`, { playerId, reason });
    },

    // Add more methods as needed based on backend endpoints
};
