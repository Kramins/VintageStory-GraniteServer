import { EventStreamClient, type ServerEvent } from './EventStreamClient';
import { handleEvent } from './EventHandlers';
export type EventHandler = (event: ServerEvent) => void;

const RETRY_BASE_MS = 1000;
const RETRY_MAX_MS = 30000;

class EventBusClass {
    private client = new EventStreamClient();
    private handlers = new Map<string, EventHandler>();
    private token: string | null = null;
    private session = 0;
    private retryTimer: ReturnType<typeof setTimeout> | null = null;
    private retryCount = 0;
    private connecting = false;

    start(token: string): void {
        if (!token) {
            this.stop();
            return;
        }

        if (this.token === token && (this.connecting || this.retryTimer)) {
            return; // already attempting/connected with this token
        }

        this.stopConnection();
        this.token = token;
        this.session += 1;
        this.retryCount = 0;
        const sessionId = this.session;
        this.connect(sessionId, token);
    }

    stop(): void {
        this.session += 1; // invalidate in-flight connections
        this.token = null;
        this.retryCount = 0;
        if (this.retryTimer) {
            clearTimeout(this.retryTimer);
            this.retryTimer = null;
        }
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
        if (this.retryTimer) {
            clearTimeout(this.retryTimer);
            this.retryTimer = null;
        }
        this.connecting = false;
        this.client.disconnect();
    }

    private scheduleReconnect(sessionId: number): void {
        if (sessionId !== this.session || !this.token) return;
        if (this.retryTimer) return;

        const delay = Math.min(RETRY_BASE_MS * Math.pow(2, this.retryCount), RETRY_MAX_MS);
        this.retryCount += 1;

        this.retryTimer = setTimeout(() => {
            this.retryTimer = null;
            if (!this.token || sessionId !== this.session) return;
            this.connect(sessionId, this.token);
        }, delay);
    }

    private handleError(sessionId: number, error: Error): void {
        if (sessionId !== this.session) return;
        if (!this.token) return;

        const message = (error && error.message) ? error.message.toLowerCase() : '';
        const unauthorized = message.includes('401') || message.includes('unauthorized');
        if (unauthorized) {
            this.stop();
            return;
        }

        this.scheduleReconnect(sessionId);
    }

    private async connect(sessionId: number, token: string): Promise<void> {
        this.connecting = true;
        try {
            await this.client.connect(
                token,
                (event) => {
                    if (sessionId !== this.session) return;
                    this.emit(event);
                },
                (error) => {
                    if (sessionId !== this.session) return;
                    this.handleError(sessionId, error);
                }
            );
        } catch (error) {
            if (sessionId !== this.session) return;
            this.handleError(sessionId, error instanceof Error ? error : new Error(String(error)));
        } finally {
            this.connecting = false;
            if (sessionId === this.session && this.token) {
                this.scheduleReconnect(sessionId);
            }
        }
    }
}

export const EventBus = new EventBusClass();
