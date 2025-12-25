import axios from 'axios';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';

const API_BASE = '/api/server';

const ServerService = {
    async getStatus(): Promise<ServerStatusDTO> {
        const response = await axios.get<ServerStatusDTO>(`${API_BASE}/status/`);
        return response.data;
    },
    async announce(message: string): Promise<string> {
        const response = await axios.post<string>(`${API_BASE}/announce`, { message });
        return response.data;
    }
};

export default ServerService;
