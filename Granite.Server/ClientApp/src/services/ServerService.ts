import axios from 'axios';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';
import type { ServerDTO } from '../types/ServerDTO';
import type { JsonApiDocument } from '../types/JsonApi';

const ServerService = {
    async fetchServers(): Promise<ServerDTO[]> {
        const response = await axios.get<JsonApiDocument<ServerDTO[]>>('/api/servers');
        return response.data.data;
    },
    
    async getStatus(serverId: string): Promise<ServerStatusDTO> {
        const response = await axios.get<JsonApiDocument<ServerStatusDTO>>(`/api/${serverId}/server/status`);
        return response.data.data;
    },
    async announce(serverId: string, message: string): Promise<string> {
        const response = await axios.post<JsonApiDocument<string>>(`/api/${serverId}/server/announce`, { message });
        return response.data.data;
    }
};

export default ServerService;
