import * as signalR from '@microsoft/signalr';

// Get SignalR URL, fallback to backend URL, then to default HTTP
let API_BASE_URL = import.meta.env.VITE_SIGNALR_URL || import.meta.env.VITE_BACKEND_URL || 'http://localhost:5001';

// In development, force HTTP to avoid SSL issues
if (import.meta.env.DEV && API_BASE_URL.startsWith('https://')) {
  API_BASE_URL = API_BASE_URL.replace('https://', 'http://');
  console.log('SignalR: Converted HTTPS to HTTP for development:', API_BASE_URL);
}

class SignalRService {
  constructor() {
    this.connection = null;
    this.eventHandlers = new Map();
    this.connectionAttempts = 0;
    this.maxConnectionAttempts = 3;
    this.isConnecting = false;
  }

  connect(token) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    if (this.isConnecting) {
      console.log('SignalR: Connection already in progress, skipping');
      return Promise.resolve();
    }

    if (!token) {
      console.warn('SignalR: No token provided, skipping connection');
      return Promise.reject('No token provided');
    }

    if (this.connectionAttempts >= this.maxConnectionAttempts) {
      console.warn('SignalR: Max connection attempts reached, skipping');
      return Promise.reject('Max connection attempts reached');
    }

    this.isConnecting = true;
    this.connectionAttempts++;

    const url = `${API_BASE_URL}/hubs/notifications`;
    console.log('SignalR: Connecting to', url, `(Attempt ${this.connectionAttempts}/${this.maxConnectionAttempts})`);

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return 2000;
          }
          return null; // Stop reconnecting after 60 seconds
        }
      })
      .build();

    // Register event handlers
    this.connection.on('TaskAssigned', (data) => {
      this.emit('TaskAssigned', data);
    });

    this.connection.on('TaskUnassigned', (data) => {
      this.emit('TaskUnassigned', data);
    });

    this.connection.on('TaskCreated', (data) => {
      this.emit('TaskCreated', data);
    });

    this.connection.on('TaskUpdated', (data) => {
      this.emit('TaskUpdated', data);
    });

    this.connection.on('TaskDeleted', (data) => {
      this.emit('TaskDeleted', data);
    });

    this.connection.on('TaskOptionCreated', (data) => {
      this.emit('TaskOptionCreated', data);
    });

    this.connection.on('TaskOptionUpdated', (data) => {
      this.emit('TaskOptionUpdated', data);
    });

    this.connection.on('TaskOptionDeleted', (data) => {
      this.emit('TaskOptionDeleted', data);
    });

    this.connection.on('StaffInvited', (data) => {
      this.emit('StaffInvited', data);
    });

    this.connection.on('StaffInvitationResponded', (data) => {
      this.emit('StaffInvitationResponded', data);
    });

    return this.connection.start().then(() => {
      console.log('SignalR Connected successfully');
      this.connectionAttempts = 0; // Reset on success
      this.isConnecting = false;
      // Join event group if eventId is available
      const eventId = this.getEventIdFromUrl();
      if (eventId) {
        this.joinEventGroup(eventId);
      }
    }).catch(err => {
      this.isConnecting = false;
      console.error('SignalR Connection Error:', err);
      console.error('SignalR Error details:', {
        message: err.message,
        stack: err.stack,
        url: url
      });
      // Don't retry if it's a permanent error (like SSL)
      if (err.message && err.message.includes('SSL')) {
        console.error('SignalR: SSL error detected. Please use HTTP in development or configure SSL certificate.');
        this.connectionAttempts = this.maxConnectionAttempts; // Stop retrying
      }
    });
  }

  disconnect() {
    if (this.connection) {
      return this.connection.stop();
    }
    return Promise.resolve();
  }

  joinEventGroup(eventId) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection.invoke('JoinEventGroup', eventId).catch(err => {
        console.error('Error joining event group:', err);
      });
    }
  }

  leaveEventGroup(eventId) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection.invoke('LeaveEventGroup', eventId).catch(err => {
        console.error('Error leaving event group:', err);
      });
    }
  }

  on(eventName, handler) {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, []);
    }
    this.eventHandlers.get(eventName).push(handler);
  }

  off(eventName, handler) {
    if (this.eventHandlers.has(eventName)) {
      const handlers = this.eventHandlers.get(eventName);
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  emit(eventName, data) {
    if (this.eventHandlers.has(eventName)) {
      this.eventHandlers.get(eventName).forEach(handler => {
        try {
          handler(data);
        } catch (error) {
          console.error(`Error in event handler for ${eventName}:`, error);
        }
      });
    }
  }

  getEventIdFromUrl() {
    const path = window.location.pathname;
    const match = path.match(/\/event\/([^\/]+)/);
    return match ? match[1] : null;
  }

  isConnected() {
    return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();

