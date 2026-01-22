import { useEffect } from 'react';
import { EventBus } from '../services/EventBus';
import type { ServerEvent } from '../services/EventStreamClient';

export function useEventBus(handler: (event: ServerEvent) => void, deps: unknown[] = []): void {
    useEffect(() => {
        const id = EventBus.register(handler);
        return () => {
            EventBus.unregister(id);
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);
}
