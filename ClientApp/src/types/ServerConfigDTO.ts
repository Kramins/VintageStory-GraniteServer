export interface ServerConfigDTO {
    port?: number;
    serverName?: string;
    welcomeMessage?: string;
    maxClients?: number;
    password?: string;
    maxChunkRadius?: number;
    whitelistMode?: string;
    allowPvp?: boolean;
    allowFireSpread?: boolean;
    allowFallingBlocks?: boolean;
}
