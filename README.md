# GraniteServer

GraniteServer is an alpha-stage server management and web API project designed to work with the [Kramins/container-vintagestory-server](https://github.com/Kramins/container-vintagestory-server) container.

**Note:** The container is not yet published to Docker Hub. Please refer to the GitHub repository for build and usage instructions.

## Status

**Alpha** – This project is under active development and is not yet feature complete. Expect breaking changes and incomplete features.

---

## API Endpoints

### **Player Management Controller**

Base URL: `/api/players`

| Method | Endpoint                                           | Description                                             |
| ------ | -------------------------------------------------- | ------------------------------------------------------- |
| GET    | `/`                                                | List all connected players                              |
| GET    | `/:playerId`                                       | Get detailed information about a specific player        |
| GET    | `/find`                                            | Find a player by name                                   |
| GET    | `/banned`                                          | List all banned players                                 |
| GET    | `/whitelisted`                                     | List all whitelisted players                            |
| POST   | `/:playerId/whitelist`                             | Add a player to the whitelist                           |
| DELETE | `/:playerId/whitelist`                             | Remove a player from the whitelist                      |
| POST   | `/:playerId/ban`                                   | Ban a player (with optional reason and expiration date) |
| DELETE | `/:playerId/ban`                                   | Remove a player from the ban list                       |
| POST   | `/:playerId/kick`                                  | Kick a player from the server                           |
| POST   | `/:playerId/inventories/:inventoryName`            | Update a player's inventory slot                        |
| DELETE | `/:playerId/inventories/:inventoryName/:slotIndex` | Delete a player's inventory from a slot                 |

---

### **World Management Controller**

Base URL: `/api/world`

| Method | Endpoint        | Description                    |
| ------ | --------------- | ------------------------------ |
| GET    | `/collectibles` | Get all collectible items data |

---

## Roadmap

We are actively working to expand the functionality of GraniteServer. Below is a roadmap of current and planned features.

### **Player Management**

#### **Currently Developing**:

- **Display Players**: List all registered players on the server.
- **Display Online Players**: Show a list of players currently online.
- **Kick Player**: Remove a player from the server in real-time.
- **Ban Player**: Prevent specific players from joining the server.
- **Whitelist Player**: Restrict server access to approved players only.

#### **Planned Features**:

- **Track Player Activity**: Monitor the time players spend on the server and provide analytics.
- **Role Management**: Assign roles (e.g., moderator, admin) to players for elevated permissions.
- **Mute Player**: Disable communication for disruptive players.
- **Custom Player Notifications**: Send tailored messages or alerts to individual players.
- **Backup Player Data**: Allow for backup and restore options for player data and progress.

---

### **World Management**

#### **Planned Features**:

- **Backup World**: Create and store backups of the current game world.
- **Download World**: Allow downloading the complete world for local archival or transfer.
- **Upload World**: Enable uploading an external world backup to the server.
- **Regenerate World**: Build tools to regenerate the world dynamically while preserving certain user settings or assets.

---

### **Authentication**

#### **Planned Features**:

- **Basic Authentication**: Implement a straightforward username and password authentication system for accessing the server.
- **OAuth Integration**: Enable OAuth-based authentication for streamlined and secure server access using external accounts.
- **API Authentication**: Add token-based authentication for secure access to the server's API endpoints.

---

### **Basic Server Controls**

#### **Planned Features**:

- **Restart Server**: Allow the server to be restarted via the management system.
- **Stop Server**: Provide functionality to stop the server gracefully.
- **Start Server**: Enable starting the server from the management interface.
- **Chat Integration**: Enable the ability to send messages to players via the server interface and monitor player chat in real-time.

---

### **Monitoring and Analytics**

#### **Planned Features**:

- **Server Metrics Dashboard**: Display real-time server performance metrics such as CPU, memory, and disk usage.
- **Player Analytics**: Track player behavior and statistics, such as login frequency and average playtime.
- **Error Logging**: Provide access to detailed logs for diagnosing server and mod issues.
- **Alerts & Notifications**: Notify the admin for critical issues (e.g., high resource usage, unexpected shutdowns).

---

### **Mod Management**

#### **Planned Features**:

- **Install/Remove Mods**: Add the ability to dynamically install or remove mods from the server.
- **Mod Version Checker**: Notify admins when there are new versions of installed mods.
- **Compatibility Checker**: Ensure mods are compatible with the current server version or other installed mods.

---

### **Cross-Compatibility**

#### **Planned Features**:

- **External Database Support**: Enable storing server data (e.g., player records, world backups) on external systems such as MySQL or PostgreSQL.
- **Integration with External Tools**: Provide compatibility with third-party tools such as Discord bots for notifications or rich interactions.

---

### **Server Configuration Management**

#### **Currently Developing**:

- **Enable/Disable Whitelist**: Provide control to enable or disable the server's whitelist feature from the management panel.

#### **Planned Features**:

- **Edit Configuration Files**: Add a user-friendly interface for modifying the server's configuration files directly from the management system.
- **Apply Configuration Changes**: Allow reloading or applying configuration changes without restarting the server.
- **Backup Configurations**: Create backups of server configuration files for easy recovery.

---

## Usage

This project is intended to be used alongside the `kramins/vintagestory` Docker container. See the container documentation for setup instructions.

## License

GPLv3
