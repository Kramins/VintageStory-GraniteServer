# Granite Server

Standalone ASP.NET Core server that manages Vintage Story game servers via SignalR.

## Architecture

- **ASP.NET Core 9.0** web API with JWT authentication
- **SignalR** for real-time communication with Vintage Story mod
- **React SPA** (ClientApp) for web-based admin interface
- **Entity Framework Core** for database persistence

## Development

### Building

```bash
# Build server only (Debug)
dotnet build Granite.Server.csproj

# Build with ClientApp (Release)
dotnet build -c Release Granite.Server.csproj
```

### Running

```bash
# Run the server
dotnet run

# Or use VS Code task: "build-server"
```

### ClientApp Development

The React SPA is located in `ClientApp/` and is served by the ASP.NET Core server in production.

```bash
# Run Vite dev server (hot reload)
cd ClientApp
npm run dev

# Or use VS Code task: "npm dev"
```

The Vite dev server (port 3000) proxies API calls to the ASP.NET Core server (port 5000).

## Configuration

Configuration is managed through `appsettings.json` and environment variables. See [GraniteServerOptions](Configuration/GraniteServerOptions.cs) for available settings.

Key settings:
- `GraniteServer__Port` - Server port (default: 5000)
- `GraniteServer__JwtSecret` - JWT signing secret (required)
- `GraniteServer__DatabaseType` - `SQLite` or `PostgreSQL`

## Project Structure

```
Granite.Server/
├── ClientApp/          # React SPA admin interface
├── Controllers/        # Web API endpoints
├── Hubs/              # SignalR hubs
├── Services/          # Business logic
├── Handlers/          # Message bus event handlers
├── HostedServices/    # Background services
├── Middleware/        # ASP.NET middleware
└── Configuration/     # Configuration classes
```
