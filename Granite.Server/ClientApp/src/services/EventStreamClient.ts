
export interface ServerEvent {
  id: string;
  messageType: string;
  data: any;
  timestamp: string;
  source?: string;
}

const API_BASE = '/api/events';

export class EventStreamClient {
  private abortController: AbortController | null = null;

  /**
   * Connect to the server event stream and listen for events.
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
    try {
      this.abortController = new AbortController();

      const response = await fetch(API_BASE, {
        method: "GET",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Accept": "text/event-stream",
        },
        signal: this.abortController.signal,
      });

      if (!response.ok) {
        throw new Error(
          `Server returned ${response.status}: ${response.statusText}`
        );
      }

      if (!response.body) {
        throw new Error("Response body is empty");
      }

      // Parse the stream manually since EventSource cannot send Authorization headers
      await this.parseEventStream(response.body, onEvent);
    } catch (error) {
      if (error instanceof Error && error.name === "AbortError") {
        console.log("Event stream connection closed");
      } else {
        onError(
          error instanceof Error
            ? error
            : new Error(String(error))
        );
      }
    }
  }

  /**
   * Disconnect from the event stream.
   */
  disconnect(): void {
    if (this.abortController) {
      this.abortController.abort();
      this.abortController = null;
    }
  }

  /**
   * Parse the event stream from the response body.
   * Implements SSE frame parsing: id: ..., event: ..., data: ...
   */
  private async parseEventStream(
    body: ReadableStream<Uint8Array>,
    onEvent: (event: ServerEvent) => void
  ): Promise<void> {
    const reader = body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";

    try {
      while (true) {
        const { done, value } = await reader.read();

        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        // Split by double newlines (SSE frame boundaries)
        const frames = buffer.split("\n\n");

        // Process complete frames, keep incomplete one in buffer
        for (let i = 0; i < frames.length - 1; i++) {
          const frame = frames[i];
          if (frame.trim()) {
            const event = this.parseSSEFrame(frame);
            if (event) {
              onEvent(event);
            }
          }
        }

        // Keep the last (possibly incomplete) frame in buffer
        buffer = frames[frames.length - 1];
      }

      // Process any remaining buffered frame
      if (buffer.trim()) {
        const event = this.parseSSEFrame(buffer);
        if (event) {
          onEvent(event);
        }
      }
    } finally {
      reader.releaseLock();
    }
  }

  /**
   * Parse a single SSE frame into an event object.
   * Format: id: <id>\nevent: <type>\ndata: <json>\n
   */
  private parseSSEFrame(frame: string): ServerEvent | null {
    const trimmed = frame.trim();
    if (!trimmed) return null;

    // First try: the frame itself might be raw JSON (server returned JSON only)
    try {
      const parsed = JSON.parse(trimmed);
      const payload = parsed && parsed.data !== undefined ? parsed.data : parsed;
      const id = parsed && parsed.id ? String(parsed.id) : `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      const eventType = parsed && (parsed.eventType || parsed.type) ? (parsed.eventType || parsed.type) : "message";
      return {
        id,
        eventType,
        data: payload,
        timestamp: new Date().toISOString(),
        source: parsed && parsed.source,
      };
    } catch {
      // not raw JSON, fall through to SSE-style parsing
    }

    const lines = frame.split(/\r?\n/);
    let id = "";
    let eventType = "";
    const dataLines: string[] = [];

    for (const line of lines) {
      const l = line.trim();
      if (l.startsWith("id:")) {
        id = l.substring(3).trim();
      } else if (l.startsWith("event:")) {
        eventType = l.substring(6).trim();
      } else if (l.startsWith("data:")) {
        dataLines.push(l.substring(5));
      }
    }

    const dataText = dataLines.join("\n").trim();
    let parsedObj: any = null;
    let payload: any = null;
    if (dataText) {
      try {
        parsedObj = JSON.parse(dataText);
        payload = parsedObj && parsedObj.data !== undefined ? parsedObj.data : parsedObj;
      } catch {
        payload = dataText;
      }
    }

    const finalId = id || (parsedObj && parsedObj.id) ? String((parsedObj && parsedObj.id) || id) : `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const finalEventType = eventType || (parsedObj && (parsedObj.eventType || parsedObj.type)) || "message";
    const source = (parsedObj && parsedObj.source) || (payload && payload.source);

    return {
      id: finalId,
      eventType: finalEventType,
      data: payload,
      timestamp: new Date().toISOString(),
      source,
    };
  }
}

// ============================================
// USAGE EXAMPLE
// ============================================

/*
import { EventStreamClient } from './EventStreamClient';

const client = new EventStreamClient();
const authToken = localStorage.getItem('authToken') || 'your-bearer-token';

// Start listening for events
client.connect(
  authToken,
  (event) => {
    console.log('Event received:', event);

    // Handle different event types
    switch (event.eventType) {
      case 'player.join':
        console.log(`Player ${event.data.playerName} joined`);
        break;
      case 'player.leave':
        console.log(`Player ${event.data.playerName} left`);
        break;
      case 'server.save':
        console.log('Server was saved');
        break;
      default:
        console.log(`Unknown event: ${event.eventType}`);
    }
  },
  (error) => {
    console.error('Event stream error:', error);
    // Implement reconnection logic here
  }
);

// Clean up when done
window.addEventListener('beforeunload', () => {
  client.disconnect();
});
*/
