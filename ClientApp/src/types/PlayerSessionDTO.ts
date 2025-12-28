export interface PlayerSessionDTO {
  id: string;
  playerId: string;
  serverId: string;
  serverName: string;
  joinDate: string; // ISO string
  leaveDate?: string; // ISO string or undefined if active
  ipAddress: string;
  playerName: string;
  durationMinutes?: number;
  isActive: boolean;
}
