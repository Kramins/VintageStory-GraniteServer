import type { ServerEvent } from './EventStreamClient';

export type EventTypeHandler = (event: ServerEvent) => void;

const eventTypeHandlers: Record<string, EventTypeHandler> = {
    // Add specific handlers here, e.g.:
    'player.join': (event) => { console.info('Player joined', event.data); },
    'player.leave': (event) => { console.info('Player left', event.data); },
};

export function registerEventTypeHandler(eventType: string, handler: EventTypeHandler): void {
    eventTypeHandlers[eventType] = handler;
}

export function handleEvent(event: ServerEvent): void {
    const handler = eventTypeHandlers[event.eventType];
    if (handler) {
        handler(event);
    } else {
        console.debug('Unhandled server event', event.eventType, event.data);
    }
}
