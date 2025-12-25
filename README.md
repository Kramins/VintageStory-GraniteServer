# GraniteServer

GraniteServer is an alpha-stage server management and web API project designed to work with the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) container.

## Status

**Alpha** – This project is under active development and is not yet feature complete. Expect breaking changes and incomplete features.

---

## API Endpoints

### **Player Management Controller**

Base URL: `/api/players`

| Method | Endpoint                                              | Description                                             |
| ------ | ----------------------------------------------------- | ------------------------------------------------------- |
| GET    | `/`                                                   | List all connected players                              |
| GET    | `/id/:playerId`                                       | Get detailed information about a specific player        |
| GET    | `/find`                                               | Find a player by name                                   |
| GET    | `/banned`                                             | List all banned players                                 |
| GET    | `/whitelisted`                                        | List all whitelisted players                            |
| POST   | `/id/:playerId/whitelist`                             | Add a player to the whitelist                           |
| DELETE | `/id/:playerId/whitelist`                             | Remove a player from the whitelist                      |
| POST   | `/id/:playerId/ban`                                   | Ban a player (with optional reason and expiration date) |
| DELETE | `/id/:playerId/ban`                                   | Remove a player from the ban list                       |
| POST   | `/id/:playerId/kick`                                  | Kick a player from the server                           |
| POST   | `/id/:playerId/inventories/:inventoryName`            | Update a player's inventory slot                        |
| DELETE | `/id/:playerId/inventories/:inventoryName/:slotIndex` | Delete a player's inventory from a slot                 |

---

### **Server Controller**

Base URL: `/api/server`

| Method | Endpoint  | Description                                                             |
| ------ | --------- | ----------------------------------------------------------------------- |
| GET    | `/config` | Get server configuration (port, name, max clients, game rules, etc.)    |
| POST   | `/config` | Update server configuration (supports partial updates - nullable props) |

**Server Configuration Properties:**

- `Port` (int?) - Server network port
- `ServerName` (string?) - Server display name
- `WelcomeMessage` (string?) - Message shown to players on login
- `MaxClients` (int?) - Maximum concurrent players
- `Password` (string?) - Server password (null/empty to remove)
- `MaxChunkRadius` (int?) - Max chunks loaded per player
- `WhitelistMode` (string?) - Whitelist mode (e.g., "Disabled", "Enabled")
- `AllowPvP` (bool?) - Enable/disable player vs player combat
- `AllowFireSpread` (bool?) - Enable/disable fire spreading
- `AllowFallingBlocks` (bool?) - Enable/disable block physics

_Note: All properties are nullable. Only include properties you want to update in POST requests._

---

### **World Management Controller**

Base URL: `/api/world`

| Method | Endpoint        | Description                    |
| ------ | --------------- | ------------------------------ |
| GET    | `/collectibles` | Get all collectible items data |

---

## Configuration

GraniteServer is configured via the `graniteserverconfig.json` file, which is created automatically at runtime if it doesn't exist. Configuration can also be overridden using environment variables, which is especially useful when running in Docker containers.

### Configuration File

The configuration file `graniteserverconfig.json` is loaded from the mod directory at server startup. If the file doesn't exist, a default configuration is created.

### Environment Variables

All configuration properties can be overridden using environment variables with the `GS_` prefix followed by the property name in uppercase.

### Configuration Properties

| Property                       | Type   | Description                                  | Default     |
| ------------------------------ | ------ | -------------------------------------------- | ----------- |
| `Port`                         | int    | API server port                              | `5000`      |
| `AuthenticationType`           | string | Authentication method (`Basic`)              | `Basic`     |
| `JwtSecret`                    | string | Secret key for JWT token signing             | Random GUID |
| `JwtExpiryMinutes`             | int    | JWT token expiration time in minutes         | `60`        |
| `JwtRefreshTokenExpiryMinutes` | int    | JWT refresh token expiration time in minutes | `1440`      |
| `Username`                     | string | Default username for API authentication      | `admin`     |
| `Password`                     | string | Default password for API authentication      | Random GUID |

### Docker Compose Example

When running with Docker Compose, you can override configuration with environment variables:

This setup should be considered alpha and not ready for production

```yaml
version: "3.8"

services:
  vintagestory:
    image: ghcr.io/kramins/vintagestory:latest
    ports:
      - "42420:42420"
      - "5000:5000"
    environment:
      GS_PORT: 5000
      GS_AUTHENTICATIONTYPE: "Basic"
      GS_JWTSECRET: "your-secret-key-here"
      GS_JWTEXPIRMINUTES: 60
      GS_USERNAME: "admin"
      GS_PASSWORD: "your-password-here"
    volumes:
      - ./world-data:/data
```

---

## Usage

This project is intended to be used alongside the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) container. See the container documentation for setup instructions.

## License

GPLv3
