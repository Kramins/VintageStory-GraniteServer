# Granite Server

A modern web-based administration framework for Vintage Story multiplayer servers. Manage your server, players, and world directly from your browser with real-time updates and interactive tools.

## Features

### 🗺️ Interactive World Map

View your world in real-time with an interactive map powered by OpenLayers. Pan, zoom, and explore your server's terrain directly from the web interface.

![World Map](docs/images/World%20Map.png)

**Map Features:**
- Interactive pan and zoom controls
- Tile-based rendering for optimal performance
- Real-time player position tracking
- World boundary display
- Responsive design

### 👥 Player Management

- Whitelist/ban/kick players
- View active sessions and player history
- Real-time player status monitoring
- Inventory management
- Player administration tools

### ⚙️ Server Configuration

- Adjust server settings via the web UI
- Real-time configuration updates
- Server status monitoring
- Event logging and history

## Architecture

**Granite Server** consists of three main components:

- **Granite.Mod** - Vintage Story server mod handling game-side logic and world data
- **Granite.Server** - ASP.NET Core backend with REST API and SignalR hubs
- **Granite.Web.Client** - Blazor WebAssembly frontend with real-time UI updates

### Technology Stack

- **Backend**: ASP.NET Core with EF Core
- **Frontend**: Blazor WebAssembly + MudBlazor UI components
- **State Management**: Fluxor
- **Real-time**: SignalR for live updates
- **Map Rendering**: OpenLayers 10
- **Database**: SQLite (dev) or PostgreSQL (production)

## License

GPLv3

## Acknowledgments

Built with ❤️ for the Vintage Story community.
