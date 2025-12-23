import axios from 'axios';
import type { ServerConfigDTO } from '../types/ServerConfigDTO';

const API_BASE = '/api/server';

export const ServerConfigurationService = {
    async getConfig(): Promise<ServerConfigDTO> {
        const response = await axios.get<ServerConfigDTO>(`${API_BASE}/config`);
        return response.data;
    },

    async updateConfig(config: ServerConfigDTO): Promise<void> {
        await axios.post(`${API_BASE}/config`, config);
    },

    async updateServerName(serverName: string): Promise<void> {
        await this.updateConfig({ serverName });
    },

    async updateWelcomeMessage(welcomeMessage: string): Promise<void> {
        await this.updateConfig({ welcomeMessage });
    },

    async updateMaxClients(maxClients: number): Promise<void> {
        await this.updateConfig({ maxClients });
    },

    async updatePassword(password: string): Promise<void> {
        await this.updateConfig({ password });
    },

    async updateMaxChunkRadius(maxChunkRadius: number): Promise<void> {
        await this.updateConfig({ maxChunkRadius });
    },

    async updateWhitelistMode(whitelistMode: string): Promise<void> {
        await this.updateConfig({ whitelistMode });
    },

    async updateAllowPvp(allowPvp: boolean): Promise<void> {
        await this.updateConfig({ allowPvp });
    },

    async updateAllowFireSpread(allowFireSpread: boolean): Promise<void> {
        await this.updateConfig({ allowFireSpread });
    },

    async updateAllowFallingBlocks(allowFallingBlocks: boolean): Promise<void> {
        await this.updateConfig({ allowFallingBlocks });
    },
};
