
import axios from 'axios';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';

const API_BASE = '/api/server';

const ServerService = {
    async getStatus(): Promise<ServerStatusDTO> {
        const response = await axios.get<ServerStatusDTO>(`${API_BASE}/status/`);
        return response.data;
    },

    async announce(message: string): Promise<any> {
        return axios.post(`${API_BASE}/announce/`, { message });
    },

    

    async stopServer(exitCode?: number): Promise<any> {
        return axios.post(`${API_BASE}/stop/`, { exitCode });
    },

    async reloadConfiguration(): Promise<any> {
        return axios.post(`${API_BASE}/reload/`);
    }
};

export default ServerService;
