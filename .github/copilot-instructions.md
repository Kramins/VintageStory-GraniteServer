Global Instructions
---
- Linter will reformat code on save. don't worry about formatting or if the file looks different.
- Use dotnet build to build the project.
- Don't use the tasks in the .vscode/tasks.json file

Granite Server Instructions
---
- Logging is provided by Microsoft.Extensions.Logging. Use this for all logging needs.
- Controller responses should use IActionResult with JsonApiDocument<T> where T is the response DTO type.
- Database operations should only be done in a service class, not in controllers.
- EF Core helper script, this will work against both sqlite and postgresql databases:
  - ./scripts/ef-migration.sh list
  - ./scripts/ef-migration.sh remove v100
  - ./scripts/ef-migration.sh create v100
  - Database version format is single letter followed by three digits, e.g., v100, v101, v102, etc.
  - Database migration creation will be done by the User and not the AI.

Granite Mod Instructions
---
- Logging is provided by Vintagestory.API.Common.ILogger. Use this for all logging needs.
- When creating ICommandHandler<T> don't create a separate class for each related command. Group them together into a single class and implement multiple ICommandHandler<T> interfaces. Example: PlayerCommandHandlers

Granite Client (Blazor) Instructions
---
- This is a Blazor application using C#. Follow standard Blazor and C# best practices.
- Use Fluxor for state management.
- MudBlazor is used for UI components.
- SignalR is used for real-time events.
- Update Fluxor state based on SignalR events to keep the UI in sync with server state.