Global Instructions
---
- Linter will reformat code on save. don't worry about formatting or if the file looks different.
- Use dotnet build to build the project.

Granite Server Instructions
---
- Logging is provided by Microsoft.Extensions.Logging. Use this for all logging needs.
- Controller responses should use IActionResult with JsonApiDocument<T> where T is the response DTO type.
- EF Core helper script, this will work against both sqlite and postgresql databases:
  - ./scripts/ef-migration.sh list
  - ./scripts/ef-migration.sh remove
  - ./scripts/ef-migration.sh create v1.2.3

Granite Mod Instructions
---
- Logging is provided by Vintagestory.API.Common.ILogger. Use this for all logging needs.
