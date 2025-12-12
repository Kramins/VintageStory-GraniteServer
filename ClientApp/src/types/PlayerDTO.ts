// PlayerDTO type for player management
export interface PlayerDTO {
    id: any;
    name: any;
    isAdmin: boolean;
    ipAddress: string;
    languageCode: string;
    ping: number;
    rolesCode: string;
    firstJoinDate: string;
    lastJoinDate: string;
    privileges: string[];
}
