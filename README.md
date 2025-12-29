# GraniteServer

GraniteServer is an alpha-stage server management and web API project designed to work with the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) container.

## Status

**Alpha** – This project is under active development and is not yet feature complete. Expect breaking changes and incomplete features.

---

## API Endpoints

### **Player Management Controller**

Base URL: `/api/players`

| Method | Endpoint                                              | Description                                              |
| ------ | ----------------------------------------------------- | -------------------------------------------------------- |
| GET    | `/`                                                   | List all players with pagination, filtering, and sorting |
| GET    | `/id/:playerId`                                       | Get detailed information about a specific player         |
| GET    | `/id/:playerId/sessions`                              | Get player session history (paginated)                   |
| GET    | `/find`                                               | Find a player by name                                    |
| POST   | `/id/:playerId/whitelist`                             | Add a player to the whitelist                            |
| DELETE | `/id/:playerId/whitelist`                             | Remove a player from the whitelist                       |
| POST   | `/id/:playerId/ban`                                   | Ban a player                                             |
| DELETE | `/id/:playerId/ban`                                   | Remove a player from the ban list                        |
| POST   | `/id/:playerId/kick`                                  | Kick a player from the server                            |
| POST   | `/id/:playerId/inventories/:inventoryName`            | Update a player's inventory slot                         |
| DELETE | `/id/:playerId/inventories/:inventoryName/:slotIndex` | Delete a player's inventory from a slot                  |

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

#### Database Configuration

- **Switch providers**: Set `GS_DATABASETYPE` to either `PostgreSQL` or `SQLite`.
- **PostgreSQL**: Provide `GS_DATABASEHOST`, `GS_DATABASEPORT` (default `5432`), `GS_DATABASENAME`, `GS_DATABASEUSERNAME`, `GS_DATABASEPASSWORD`.
- **SQLite**: Set `GS_DATABASENAME` to specify the database file name (default: `graniteserver`). The `.db` extension is added automatically if not present. The database file is created in the server's data directory.

Notes:

- For development, the Postgres service in the dev container is already wired via `GS_DATABASEHOST=db`.
- The mod avoids auto-deleting the SQLite file on startup. Postgres databases may be reset by the app during development.

### Configuration Properties

| Property                       | Type   | Description                                  | Default         |
| ------------------------------ | ------ | -------------------------------------------- | --------------- |
| `ServerId`                     | Guid   | Unique server identifier                     | Random GUID     |
| `Port`                         | int    | API server port                              | `5000`          |
| `AuthenticationType`           | string | Authentication method (`Basic`)              | `Basic`         |
| `JwtSecret`                    | string | Secret key for JWT token signing             | Random GUID     |
| `JwtExpiryMinutes`             | int    | JWT token expiration time in minutes         | `60`            |
| `JwtRefreshTokenExpiryMinutes` | int    | JWT refresh token expiration time in minutes | `1440`          |
| `Username`                     | string | Default username for API authentication      | `admin`         |
| `Password`                     | string | Default password for API authentication      | Random GUID     |
| `DatabaseType`                 | string | Database provider (`SQLite` or `PostgreSQL`) | `Sqlite`        |
| `DatabaseHost`                 | string | Database server host (PostgreSQL only)       | `null`          |
| `DatabasePort`                 | int    | Database server port (PostgreSQL only)       | `5432`          |
| `DatabaseName`                 | string | Database name                                | `graniteserver` |
| `DatabaseUsername`             | string | Database username (PostgreSQL only)          | `null`          |
| `DatabasePassword`             | string | Database password (PostgreSQL only)          | `null`          |

### Docker Compose Examples

When running with Docker Compose, you can override configuration with environment variables.

**Note:** These setups should be considered alpha and not ready for production.

#### Example 1: SQLite (Simple Setup)

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
      GS_DATABASETYPE: "SQLite"
      GS_AUTHENTICATIONTYPE: "Basic"
      GS_JWTEXPIRYMINUTES: 60
      GS_USERNAME: "admin"
      # Optional, if not set it will be generated
      GS_JWTSECRET: "your-secret-key-here"
      GS_PASSWORD: "your-password-here"
    volumes:
      - ./world-data:/data
```

#### Example 2: PostgreSQL (Production-Ready Database)

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
      GS_DATABASETYPE: "PostgreSQL"
      GS_DATABASEHOST: "postgres"
      GS_DATABASEPORT: 5432
      GS_DATABASENAME: "graniteserver"
      GS_DATABASEUSERNAME: "postgres"
      GS_DATABASEPASSWORD: "your-postgres-password"
      GS_AUTHENTICATIONTYPE: "Basic"
      GS_JWTEXPIRYMINUTES: 60
      GS_USERNAME: "admin"
      # Optional, if not set it will be generated
      GS_JWTSECRET: "your-secret-key-here"
      GS_PASSWORD: "your-password-here"
    volumes:
      - ./world-data:/data
    depends_on:
      - postgres

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: graniteserver
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: your-postgres-password
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres-data:
```

---

## Usage

This project is intended to be used alongside the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) container. See the container documentation for setup instructions.

## License

GPLv3
