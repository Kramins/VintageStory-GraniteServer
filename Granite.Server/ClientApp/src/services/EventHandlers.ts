import type { ServerEvent } from './EventStreamClient';

export type EventTypeHandler = (event: ServerEvent) => void;

const eventTypeHandlers: Record<string, EventTypeHandler> = {
    // Server emits MessageType names like PlayerJoinedEvent / PlayerLeaveEvent / ServerMetricsEvent
    PlayerJoinedEvent: (event) => { console.info('Player joined', event.data); },
    PlayerLeaveEvent: (event) => { console.info('Player left', event.data); },
    ServerMetricsEvent: (event) => { console.debug('Server metrics', event.data); },
};

export function registerEventTypeHandler(eventType: string, handler: EventTypeHandler): void {
    eventTypeHandlers[eventType] = handler;
}

export function handleEvent(event: ServerEvent): void {
    const handler = eventTypeHandlers[event.messageType];
    if (handler) {
        handler(event);
    } else {
        console.debug('Unhandled server event', event.messageType, event.data);
    }
}
