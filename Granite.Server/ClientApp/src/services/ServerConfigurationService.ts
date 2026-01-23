import axios from 'axios';
import type { ServerConfigDTO } from '../types/ServerConfigDTO';
import type { JsonApiDocument } from '../types/JsonApi';

export const ServerConfigurationService = {
    async getConfig(serverId: string): Promise<ServerConfigDTO> {
        const response = await axios.get<JsonApiDocument<ServerConfigDTO>>(`/api/${serverId}/config`);
        return response.data.data;
    },

    async updateConfig(serverId: string, config: ServerConfigDTO): Promise<void> {
        await axios.patch<JsonApiDocument<string>>(`/api/${serverId}/config`, config);
    },

    async syncConfig(serverId: string): Promise<void> {
        await axios.post(`/api/${serverId}/config/sync`);
    },

    async updateServerName(serverId: string, serverName: string): Promise<void> {
        await this.updateConfig(serverId, { serverName });
    },

    async updateWelcomeMessage(serverId: string, welcomeMessage: string): Promise<void> {
        await this.updateConfig(serverId, { welcomeMessage });
    },

    async updateMaxClients(serverId: string, maxClients: number): Promise<void> {
        await this.updateConfig(serverId, { maxClients });
    },

    async updatePassword(serverId: string, password: string): Promise<void> {
        await this.updateConfig(serverId, { password });
    },

    async updateMaxChunkRadius(serverId: string, maxChunkRadius: number): Promise<void> {
        await this.updateConfig(serverId, { maxChunkRadius });
    },

    async updateWhitelistMode(serverId: string, whitelistMode: string): Promise<void> {
        await this.updateConfig(serverId, { whitelistMode });
    },

    async updateAllowPvP(serverId: string, allowPvP: boolean): Promise<void> {
        await this.updateConfig(serverId, { allowPvP });
    },

    async updateAllowFireSpread(serverId: string, allowFireSpread: boolean): Promise<void> {
        await this.updateConfig(serverId, { allowFireSpread });
    },

    async updateAllowFallingBlocks(serverId: string, allowFallingBlocks: boolean): Promise<void> {
        await this.updateConfig(serverId, { allowFallingBlocks });
    },
};
