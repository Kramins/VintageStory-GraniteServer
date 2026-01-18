import axios from 'axios';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';
import type { ServerDTO } from '../types/ServerDTO';
import type { JsonApiDocument } from '../types/JsonApi';

const API_BASE = '/api/server';

const ServerService = {
    async fetchServers(): Promise<ServerDTO[]> {
        const response = await axios.get<JsonApiDocument<ServerDTO[]>>('/api/servers');
        return response.data.data;
    },
    
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
