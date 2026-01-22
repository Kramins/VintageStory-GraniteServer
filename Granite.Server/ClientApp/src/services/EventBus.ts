import { SignalRClient } from './SignalRClient';
import type { ServerEvent } from './EventStreamClient';
import { handleEvent } from './EventHandlers';
export type EventHandler = (event: ServerEvent) => void;

class EventBusClass {
    private client = new SignalRClient();
    private handlers = new Map<string, EventHandler>();
    private token: string | null = null;
    private connecting = false;

    start(token: string): void {
        if (!token) {
            this.stop();
            return;
        }

        if (this.token === token && this.connecting) {
            return; // already attempting/connected with this token
        }

        this.stopConnection();
        this.token = token;
        this.connect(token);
    }

    stop(): void {
        this.token = null;
        this.connecting = false;
        this.client.disconnect();
    }

    register(handler: EventHandler): string {
        const id = `h_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
        this.handlers.set(id, handler);
        return id;
    }

    unregister(id: string): void {
        this.handlers.delete(id);
    }

    emit(event: ServerEvent): void {
        try {
            handleEvent(event);
        } catch (error) {
            console.error('Error handling server event', error, event);
        }

        for (const handler of this.handlers.values()) {
            try {
                handler(event);
            } catch (error) {
                console.error('Error in event handler', error, event);
            }
        }
    }

    private stopConnection(): void {
        this.connecting = false;
        this.client.disconnect();
    }

    private async connect(token: string): Promise<void> {
        if (!token) return;
        
        this.connecting = true;
        try {
            await this.client.connect(
                token,
                (event) => {
                    if (this.token === token) {
                        this.emit(event);
                    }
                },
                (error) => {
                    // SignalR handles reconnection automatically
                    // Just log errors
                    console.error('SignalR error:', error);
                }
            );
        } catch (error) {
            console.error('Failed to connect to SignalR:', error);
        } finally {
            this.connecting = false;
        }
    }
}

export const EventBus = new EventBusClass();
