
import axios from 'axios';
import type { CollectibleObjectDTO } from '../types/CollectibleObjectDTO';


const API_BASE = '/api/world';

const WorldService = {
    async GetAllCollectibles(): Promise<CollectibleObjectDTO[]> {
        const response = await axios.get<CollectibleObjectDTO[]>(`${API_BASE}/collectibles`);
        return response.data;
    },

 
};

export default WorldService;
