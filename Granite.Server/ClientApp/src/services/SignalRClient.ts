import * as signalR from '@microsoft/signalr';
import type { ServerEvent } from './EventStreamClient';

export class SignalRClient {
    private connection: signalR.HubConnection | null = null;
    private onEventCallback: ((event: ServerEvent) => void) | null = null;
    private onErrorCallback: ((error: Error) => void) | null = null;

    /**
     * Connect to the SignalR hub and listen for events.
     *
     * @param token The Bearer authentication token
     * @param onEvent Callback function when an event is received
     * @param onError Callback function when an error occurs
     */
    async connect(
        token: string,
        onEvent: (event: ServerEvent) => void,
        onError: (error: Error) => void
    ): Promise<void> {
        this.onEventCallback = onEvent;
        this.onErrorCallback = onError;

        try {
            // Create connection to ClientHub
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hub/client', {
                    accessTokenFactory: () => token,
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 30s (max)
                        const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                        return delay;
                    },
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Register event handlers
            this.connection.on('ServerEvent', (event: ServerEvent) => {
                if (this.onEventCallback) {
                    this.onEventCallback(event);
                }
            });

            // Handle connection lifecycle
            this.connection.onreconnecting((error) => {
                console.warn('SignalR connection lost. Reconnecting...', error);
            });

            this.connection.onreconnected((connectionId) => {
                console.log('SignalR reconnected:', connectionId);
            });

            this.connection.onclose((error) => {
                console.error('SignalR connection closed:', error);
                if (error && this.onErrorCallback) {
                    this.onErrorCallback(error);
                }
            });

            // Start the connection
            await this.connection.start();
            console.log('SignalR connected:', this.connection.connectionId);
        } catch (error) {
            const err = error instanceof Error ? error : new Error(String(error));
            console.error('SignalR connection failed:', err);
            if (this.onErrorCallback) {
                this.onErrorCallback(err);
            }
            throw err;
        }
    }

    /**
     * Disconnect from the SignalR hub.
     */
    async disconnect(): Promise<void> {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log('SignalR disconnected');
            } catch (error) {
                console.error('Error disconnecting SignalR:', error);
            } finally {
                this.connection = null;
                this.onEventCallback = null;
                this.onErrorCallback = null;
            }
        }
    }

    /**
     * Check if currently connected to the hub.
     */
    isConnected(): boolean {
        return this.connection?.state === signalR.HubConnectionState.Connected;
    }

    /**
     * Get the current connection state.
     */
    getState(): signalR.HubConnectionState | null {
        return this.connection?.state ?? null;
    }
}
