# GraniteServer

> **Note:** This project is in early alpha and shouldn't be used in production.

GraniteServer provides a modern web-based administration panel for Vintage Story servers. Built with a Blazor WebAssembly frontend and ASP.NET Core backend, it offers real-time server monitoring, player management, world administration, and interactive world map viewing accessible through any web browser.

The system consists of two main components:
- **Granite.Server**: Standalone ASP.NET Core 9.0 web API with SignalR for real-time communication
- **Granite.Mod**: Vintage Story server mod that integrates with the Granite.Server backend

## Architecture

- **Backend**: ASP.NET Core 9.0 web API with JWT authentication and SignalR for real-time updates
- **Frontend**: Blazor WebAssembly with C# using Fluxor for state management and MudBlazor for UI components
- **Database**: Entity Framework Core with PostgreSQL or SQLite support
- **Messaging**: CQRS-inspired, event-driven architecture with in-process message bus
- **Container Support**: Designed to work with the Vintage Story container [ghcr.io/kramins/vintagestory:latest](https://github.com/Kramins/container-vintagestory-server)

## Current Features

- **Multi-server management**: Manage multiple Vintage Story servers from a single interface
- **Player management**: Whitelist, ban, kick, and detailed player sessions
- **Server configuration**: Change server settings from the web UI
- **Interactive world map**: View your Vintage Story world with OpenLayers-powered map tiles
- **Real-time updates**: SignalR-powered live server status and events
- **User administration**: Multi-user support with role-based access

## Features in Development

- **Player inventory management**: View and manage player inventories (TBD)
- **Mod management**: Install and manage server mods (TBD)

## Screenshots

### Interactive World Map

![World Map](docs/images/World%20Map.png)

---

## Quickstart (Dev Container)

The recommended way to develop GraniteServer is using the included dev container in VS Code. This provides a pre-configured environment with all dependencies.

### 1. Open in VS Code

- Use "Open Folder in Container" or "Reopen in Container" if prompted.

### 2. Build the project

Use the VS Code tasks (available in the Command Palette or Tasks menu):

- **build-all**: Builds all projects using Cake build script
- **build-server**: Builds Granite.Server
- **build-mod**: Builds Granite.Mod
- **build-blazor-client**: Builds Granite.Web.Client

Or build manually:

```bash
# Build all projects
dotnet cake.cs -- --target Build --configuration Debug

# Or build individual components
dotnet build Granite.Server/Granite.Server.csproj
dotnet build Granite.Mod/Granite.Mod.csproj
dotnet build Granite.Web.Client/Granite.Web.Client.csproj
```

### 3. Running for Development

Use the VS Code **Run and Debug** panel (Ctrl+Shift+D) with these launch configurations:

- **Launch Granite Server (Postgres)**: Starts the backend with PostgreSQL
- **Launch Granite Server (SQLite)**: Starts the backend with SQLite
- **Launch Mod**: Starts a Vintage Story server with the Granite.Mod loaded
- **Launch Blazor Client**: Starts the Blazor WebAssembly frontend dev server
- **Mod + Granite Server (Postgres)**: Compound configuration that launches both
- **Mod + Granite Server (SQLite)**: Compound configuration with SQLite

Or run manually:

```bash
# Granite.Server (Backend)
cd Granite.Server
dotnet run
# Available at http://localhost:5000

# Granite.Web.Client (Frontend)
cd Granite.Web.Client
dotnet watch
# Available at https://localhost:7171
```

---

## Configuration

### Granite.Server Configuration

Configuration is managed through `appsettings.json` and environment variables. Key settings:

- **Database Type**: `GraniteServer:DatabaseType` (`PostgreSQL` or `SQLite`)
- **PostgreSQL**: Set connection string via `ConnectionStrings:DefaultConnection`
- **SQLite**: Database file will be created in the server directory
- **JWT Settings**: Configure `GraniteServer:JwtSecret`, `GraniteServer:JwtExpiryMinutes`
- **Authentication**: Configure default admin user via `GraniteServer:DefaultAdminUsername` and `GraniteServer:DefaultAdminPassword`

### Granite.Mod Configuration

The mod configuration is stored in the Vintage Story mod directory. See the mod's documentation for connecting it to the Granite.Server backend.

---

## Docker Deployment

The following Docker Compose example shows how to deploy GraniteServer with a Vintage Story server using the container image.

> **Note:** The environment variables below use the `GS_` prefix which is specific to the containerized deployment. For standalone server configuration, refer to the Configuration section above.

```yaml
version: "3.8"

services:
  vintagestory:
    image: ghcr.io/kramins/vintagestory:latest
    ports:
      - "42420:42420"     # Vintage Story Game Server
    environment:
      # Vintage Story and Mod Configuration
      - VINTAGE_STORY=/app
      - GS_SERVERID=your-unique-server-id
      - GS_ACCESSTOKEN=your-access-token
      - GS_GRANITESERVERHOST=http://localhost:5000
    volumes:
      - vintage-data:/data

  granite-server:
    image: ghcr.io/kramins/<TBD>
    ports:
      - "5000:5000"       # Granite Server Web UI/API
    environment:
      # Granite Server Configuration
      - GS_JWTSECRET=your-jwt-secret-key-min-32-chars
      - GS_USERNAME=admin
      - GS_PASSWORD=YourSecurePassword123!
      - GS_REGISTRATIONENABLED=true
      - GS_REQUIREAPPROVAL=true
      
      # Database Configuration (PostgreSQL)
      - GS_DATABASETYPE=PostgreSQL
      - GS_DATABASEHOST=postgres
      - GS_DATABASEUSERNAME=postgres
      - GS_DATABASEPASSWORD=your-postgres-password
      - GS_DATABASEPORT=5432
      - GS_DATABASENAME=graniteserver
      
      # Auto Configuration (matches SERVERID and ACCESSTOKEN above)
      - GS_GRANITEMODSERVERID=your-unique-server-id
      - GS_GRANITEMODTOKEN=your-access-token
    depends_on:
      - postgres
  postgres:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: your-postgres-password
      POSTGRES_DB: graniteserver
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  vintage-data:
  postgres-data:
```

### Environment Variables

**Authentication:**
- `GS_REGISTRATIONENABLED`: Allow new user registrations
- `GS_REQUIREAPPROVAL`: Require admin approval for new users

**Database:**
- `GS_DATABASETYPE`: `PostgreSQL` or `SQLite`
- For SQLite, omit the PostgreSQL-specific variables

**Security:**
- Always use strong passwords and secrets in production
- Generate unique `GS_SERVERID` and `GS_ACCESSTOKEN` for each deployment

---

## Usage

GraniteServer consists of two main components that work together:

1. **Granite.Server**: The standalone ASP.NET Core backend that provides the web API and admin interface
2. **Granite.Mod**: The Vintage Story server mod that connects to the Granite.Server backend

### Deployment Options:

- **Containerized**: Use the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) Docker image which includes both the Vintage Story server and Granite.Mod pre-configured
- **Standalone**: Deploy Granite.Server separately and install Granite.Mod on your Vintage Story server, then configure the mod to connect to your Granite.Server instance

## Contributing

This project uses a CQRS-inspired, event-driven architecture. See [Design.md](Design.md) for architectural details and development guidelines.

## License

GPLv3

---

*Generative AI has been used to assist with the creation of this project.*
