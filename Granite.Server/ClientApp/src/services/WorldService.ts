import axios from 'axios';
import type { CollectibleObjectDTO } from '../types/CollectibleObjectDTO';
import type { JsonApiDocument } from '../types/JsonApi';

const WorldService = {
    async GetAllCollectibles(serverId: string): Promise<CollectibleObjectDTO[]> {
        const response = await axios.get<JsonApiDocument<CollectibleObjectDTO[]>>(`/api/${serverId}/world/collectibles`);
        return response.data.data;
    },
    
    async saveNow(serverId: string): Promise<any> {
        const response = await axios.post<JsonApiDocument<any>>(`/api/${serverId}/world/save`);
        return response.data.data;
    }
};

export default WorldService;
