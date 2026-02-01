# Granite Server

Standalone ASP.NET Core server that manages Vintage Story game servers via SignalR.

## Architecture

- **ASP.NET Core 9.0** web API with JWT authentication
- **SignalR** for real-time communication with Vintage Story mod
- **Blazor WebAssembly** (Granite.Web.Client) for web-based admin interface
- **Entity Framework Core** for database persistence

## Development

### Building

```bash
# Build server only (Debug)
dotnet build Granite.Server.csproj

# Build with Blazor client (Release)
dotnet build -c Release Granite.Server.csproj
```

### Running

```bash
# Run the server
dotnet run

# Or use VS Code task: "build-server"
```

### Blazor Client Development

The Blazor WebAssembly client is located in `../Granite.Web.Client/` and is served by the ASP.NET Core server in production.

Development workflow (separate dev servers):
1. Start Granite.Server: `dotnet run` in Granite.Server folder (serves API on port 5000)
2. Start Blazor Client: `dotnet watch` in Granite.Web.Client folder (dev server on port 5148/7171)
3. Navigate to `https://localhost:7171` for development

For integrated builds with Blazor client included in the server:
```bash
# Publish release build with Blazor client included
dotnet publish -c Release Granite.Server.csproj /p:BuildWebApp=true
```

## Configuration

Configuration is managed through `appsettings.json` and environment variables. See [GraniteServerOptions](Configuration/GraniteServerOptions.cs) for available settings.

Key settings:
- `GraniteServer__Port` - Server port (default: 5000)
- `GraniteServer__JwtSecret` - JWT signing secret (required)
- `GraniteServer__DatabaseType` - `SQLite` or `PostgreSQL`

## Project Structure

```
Granite.Server/
├── Controllers/        # Web API endpoints
├── Hubs/              # SignalR hubs
├── Services/          # Business logic
├── Handlers/          # Message bus event handlers
├── HostedServices/    # Background services
├── Middleware/        # ASP.NET middleware
├── Configuration/     # Configuration classes
└── wwwroot/           # Static files (Blazor client output in production)
```
