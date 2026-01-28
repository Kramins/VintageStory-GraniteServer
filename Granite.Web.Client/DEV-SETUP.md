# Development Setup - Blazor Client with API Proxy

## Configuration Summary

The Blazor WebAssembly client is configured to proxy API requests to Granite.Server in development.

### Ports
- **Granite.Server (API)**: `http://localhost:5000`
- **Blazor WebAssembly**: `http://localhost:5148` (http) or `https://localhost:7171` (https)

### Files Configured

1. **Granite.Web.Client/wwwroot/appsettings.Development.json**
   ```json
   {
     "ApiBaseUrl": "http://localhost:5000"
   }
   ```

2. **Granite.Server/Program.cs**
   - CORS configured to allow requests from Blazor dev server origins:
     - `http://localhost:5148`
     - `https://localhost:7171`

## Running in Development

### Terminal 1: Start Granite.Server (API)
```bash
cd /workspaces/VintageStory-GraniteServer/Granite.Server
dotnet run
```
The API will be available at: `http://localhost:5000`

### Terminal 2: Start Blazor Client
```bash
cd /workspaces/VintageStory-GraniteServer/Granite.Web.Client
dotnet watch
```
The Blazor app will be available at: `http://localhost:5148`

## How It Works

1. The Blazor client reads `ApiBaseUrl` from `appsettings.Development.json`
2. All API requests are made to `http://localhost:5000`
3. Granite.Server CORS policy allows requests from the Blazor dev server
4. No proxy middleware needed - direct HTTP requests with CORS

## VS Code Tasks

You can use the existing tasks to build:
- **Task**: `build-server` - Builds Granite.Server
- **Task**: `npm dev` - Runs the React ClientApp (legacy)

## Testing the Setup

1. Start Granite.Server: `dotnet run` in Granite.Server folder
2. Start Blazor Client: `dotnet watch` in Granite.Web.Client folder
3. Navigate to `http://localhost:5148/login`
4. Login credentials should authenticate against `http://localhost:5000/api/auth/login`

## Production Configuration

In production, `appsettings.json` has `ApiBaseUrl: ""` which means the Blazor app will make requests to the same origin where it's hosted.
