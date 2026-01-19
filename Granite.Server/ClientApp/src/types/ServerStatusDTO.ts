export interface ServerStatusDTO {
    serverIp: string;
    upTime: number;
    currentPlayers: number;
    maxPlayers: number;
    serverName: string;
    gameVersion: string;
    worldAgeDays: number;
    memoryUsageBytes: number;
    isOnline: boolean;
    worldName: string;
    worldSeed: number;
}