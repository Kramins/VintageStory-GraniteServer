// PlayerDTO type for player management
export interface PlayerDTO {
    id: string;
    playerUID: string;
    serverId: string;
    name: string;
    isAdmin: boolean;
    ipAddress: string;
    languageCode: string;
    ping: number;
    rolesCode: string;
    firstJoinDate: string;
    lastJoinDate: string;
    privileges: string[];
    connectionState: string;
    isBanned: boolean;
    isWhitelisted: boolean;
    banReason?: string | null;
    banBy?: string | null;
    banUntil?: string | null;
    whitelistedReason?: string | null;
    whitelistedBy?: string | null;
    whitelistedUntil?: string | null;
}
