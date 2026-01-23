import type { AppDispatch } from '../store/store';
import {
    playerUpdated,
    playerBanned,
    playerUnbanned,
    playerWhitelisted,
    playerUnwhitelisted,
    playerJoined,
    playerLeft,
} from '../store/slices/playersSlice';
import { PlayerService } from './PlayerService';
import type { ServerEvent } from './EventStreamClient';
import type { PlayerDTO } from '../types/PlayerDTO';
import type { PlayerDetailsDTO } from '../types/PlayerDetailsDTO';

/**
 * Handles player-related events from the server and updates Redux state
 * Also fetches the latest player data to ensure consistency
 */
export class PlayerEventHandlers {
    private dispatch: AppDispatch;

    constructor(dispatch: AppDispatch) {
        this.dispatch = dispatch;
    }

    /**
     * Handle player banned event
     */
    async handlePlayerBanned(event: ServerEvent): Promise<void> {
        const data = event.data;
        if (!data?.playerId || !data?.serverId) {
            console.warn('Invalid PlayerBannedEvent data', data);
            return;
        }

        // Immediately update Redux state
        this.dispatch(playerBanned({ playerId: data.playerId, serverId: data.serverId }));

        // Fetch updated player data to ensure consistency
        try {
            const updatedPlayer = await PlayerService.getPlayerDetails(data.serverId, data.playerId);
            this.dispatch(playerUpdated(this.dtoToPlayer(updatedPlayer, data.serverId)));
        } catch (error) {
            console.error('Failed to fetch updated player after ban event', error);
        }
    }

    /**
     * Handle player unbanned event
     */
    async handlePlayerUnbanned(event: ServerEvent): Promise<void> {
        const data = event.data;
        if (!data?.playerId || !data?.serverId) {
            console.warn('Invalid PlayerUnbannedEvent data', data);
            return;
        }

        // Immediately update Redux state
        this.dispatch(playerUnbanned({ playerId: data.playerId, serverId: data.serverId }));

        // Fetch updated player data to ensure consistency
        try {
            const updatedPlayer = await PlayerService.getPlayerDetails(data.serverId, data.playerId);
            this.dispatch(playerUpdated(this.dtoToPlayer(updatedPlayer, data.serverId)));
        } catch (error) {
            console.error('Failed to fetch updated player after unban event', error);
        }
    }

    /**
     * Handle player whitelisted event
     */
    async handlePlayerWhitelisted(event: ServerEvent): Promise<void> {
        const data = event.data;
        if (!data?.playerId || !data?.serverId) {
            console.warn('Invalid PlayerWhitelistedEvent data', data);
            return;
        }

        // Immediately update Redux state
        this.dispatch(playerWhitelisted({ playerId: data.playerId, serverId: data.serverId }));

        // Fetch updated player data to ensure consistency
        try {
            const updatedPlayer = await PlayerService.getPlayerDetails(data.serverId, data.playerId);
            this.dispatch(playerUpdated(this.dtoToPlayer(updatedPlayer, data.serverId)));
        } catch (error) {
            console.error('Failed to fetch updated player after whitelist event', error);
        }
    }

    /**
     * Handle player unwhitelisted event
     */
    async handlePlayerUnwhitelisted(event: ServerEvent): Promise<void> {
        const data = event.data;
        if (!data?.playerId || !data?.serverId) {
            console.warn('Invalid PlayerUnwhitelistedEvent data', data);
            return;
        }

        // Immediately update Redux state
        this.dispatch(playerUnwhitelisted({ playerId: data.playerId, serverId: data.serverId }));

        // Fetch updated player data to ensure consistency
        try {
            const updatedPlayer = await PlayerService.getPlayerDetails(data.serverId, data.playerId);
            this.dispatch(playerUpdated(this.dtoToPlayer(updatedPlayer, data.serverId)));
        } catch (error) {
            console.error('Failed to fetch updated player after unwhitelist event', error);
        }
    }

    /**
     * Handle player joined event
     */
    async handlePlayerJoined(event: ServerEvent): Promise<void> {
        const data = event.data;
        const serverId =
            (event as any).originServerId || (event as any).originserverid || event.source || data?.serverId;
        const playerUID = data?.playerUID ?? data?.PlayerUID ?? data?.playerUid;

        if (!serverId || !playerUID) {
            console.warn('Invalid PlayerJoinedEvent data', data);
            return;
        }

        // Try to fetch updated player list and locate by UID
        try {
            const players = await PlayerService.getAllPlayers(serverId);
            const player = players.find(p => p.playerUID === playerUID);
            if (player) {
                this.dispatch(playerJoined(player));
                return;
            }
        } catch (error) {
            console.error('Failed to refresh players after join event', error);
        }

        // Fallback: insert minimal data with Connected state
        this.dispatch(
            playerJoined({
                id: `${playerUID}-${serverId}`,
                playerUID,
                serverId,
                name: data?.playerName ?? 'Unknown',
                isAdmin: false,
                ipAddress: data?.ipAddress ?? '',
                languageCode: '',
                ping: 0,
                rolesCode: '',
                firstJoinDate: new Date().toISOString(),
                lastJoinDate: new Date().toISOString(),
                privileges: [],
                connectionState: 'Connected',
                isBanned: false,
                isWhitelisted: false,
                banReason: null,
                banBy: null,
                banUntil: null,
                whitelistedReason: null,
                whitelistedBy: null,
                whitelistedUntil: null,
            })
        );
    }

    /**
     * Handle player left event
     */
    async handlePlayerLeft(event: ServerEvent): Promise<void> {
        const data = event.data;
        const serverId =
            (event as any).originServerId || (event as any).originserverid || event.source || data?.serverId;
        const playerUID = data?.playerUID ?? data?.PlayerUID ?? data?.playerUid;

        if (!serverId || !playerUID) {
            console.warn('Invalid PlayerLeaveEvent data', data);
            return;
        }

        // Attempt to refresh player record and update connection state
        try {
            const players = await PlayerService.getAllPlayers(serverId);
            const player = players.find(p => p.playerUID === playerUID);
            if (player) {
                this.dispatch(playerUpdated({ ...player, connectionState: 'Disconnected' }));
            } else {
                this.dispatch(playerLeft({ playerUID, serverId }));
            }
        } catch (error) {
            console.error('Failed to refresh players after leave event', error);
            this.dispatch(playerLeft({ playerUID, serverId }));
        }
    }

    /**
     * Helper to convert PlayerDetailsDTO to PlayerDTO format for Redux
     */
    private dtoToPlayer(dto: PlayerDetailsDTO, serverId: string): PlayerDTO {
        return {
            id: dto.id,
            playerUID: dto.playerUID,
            serverId,
            name: dto.name,
            isAdmin: dto.isAdmin,
            ipAddress: dto.ipAddress,
            languageCode: dto.languageCode,
            ping: dto.ping,
            rolesCode: dto.rolesCode,
            firstJoinDate: dto.firstJoinDate,
            lastJoinDate: dto.lastJoinDate,
            privileges: dto.privileges,
            connectionState: dto.connectionState,
            isBanned: dto.isBanned,
            banReason: dto.banReason,
            banBy: dto.banBy,
            banUntil: dto.banUntil,
            isWhitelisted: dto.isWhitelisted,
            whitelistedReason: dto.whitelistedReason,
            whitelistedBy: dto.whitelistedBy,
            whitelistedUntil: dto.whitelistedUntil,
        };
    }
}
