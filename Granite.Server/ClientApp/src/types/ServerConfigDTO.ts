export interface ServerConfigDTO {
    port?: number;
    serverName?: string;
    welcomeMessage?: string;
    maxClients?: number;
    password?: string;
    maxChunkRadius?: number;
    whitelistMode?: string;
    allowPvP?: boolean;
    allowFireSpread?: boolean;
    allowFallingBlocks?: boolean;
}
