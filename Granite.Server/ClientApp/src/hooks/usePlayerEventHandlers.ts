import { useEffect } from 'react';
import { useAppDispatch } from '../store/store';
import { EventBus } from '../services/EventBus';
import { PlayerEventHandlers } from '../services/PlayerEventHandlers';
import type { ServerEvent } from '../services/EventStreamClient';

/**
 * React hook for registering player event handlers
 * Call this in a component that should listen for player events
 */
export function usePlayerEventHandlers(): void {
    const dispatch = useAppDispatch();

    useEffect(() => {
        const handlers = new PlayerEventHandlers(dispatch);

        // Register event handlers with the EventBus
        const banHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerBannedEvent') {
                handlers.handlePlayerBanned(event);
            }
        });

        const unbanHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerUnbannedEvent') {
                handlers.handlePlayerUnbanned(event);
            }
        });

        const whitelistHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerWhitelistedEvent') {
                handlers.handlePlayerWhitelisted(event);
            }
        });

        const unwhitelistHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerUnwhitelistedEvent') {
                handlers.handlePlayerUnwhitelisted(event);
            }
        });

        const joinHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerJoinedEvent') {
                handlers.handlePlayerJoined(event);
            }
        });

        const leaveHandler = EventBus.register((event: ServerEvent) => {
            if (event.messageType === 'PlayerLeaveEvent') {
                handlers.handlePlayerLeft(event);
            }
        });

        // Cleanup on unmount
        return () => {
            EventBus.unregister(banHandler);
            EventBus.unregister(unbanHandler);
            EventBus.unregister(whitelistHandler);
            EventBus.unregister(unwhitelistHandler);
            EventBus.unregister(joinHandler);
            EventBus.unregister(leaveHandler);
        };
    }, [dispatch]);
}
