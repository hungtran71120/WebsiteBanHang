---
name: db-migration
description: This skill should be used when the user asks to "create a migration", "add a migration for X", "update the database", "tạo migration", "update database", "thêm migration cho entity X" in the ShopeeClone project, or after scaffold-entity has generated a new entity that needs a migration.
---

Create and apply an Entity Framework Core migration for the ShopeeClone project (`E:\ProjectVuiVe\WebSiteBanHang`). Run these commands from the repository root — the project paths are fixed for this repo, do not ask the user for them.

## Step 1 — Create the migration

Ask the user for a migration name if not given (PascalCase, descriptive of the change, e.g. `AddProductCatalog`, `AddCartAndOrders`). Then run:

```
dotnet ef migrations add <MigrationName> --project src/Infrastructure/ShopeeClone.Infrastructure.csproj --startup-project src/API/ShopeeClone.API.csproj --output-dir Persistence/Migrations
```

## Step 2 — Apply it to the database

```
dotnet ef database update --project src/Infrastructure/ShopeeClone.Infrastructure.csproj --startup-project src/API/ShopeeClone.API.csproj
```

## Known issues (already hit during Phase 0 — check these first if a command fails)

- If the tool reports the startup project doesn't reference `Microsoft.EntityFrameworkCore.Design`, that package must be present in **both** `src/API/ShopeeClone.API.csproj` (the startup project) and `src/Infrastructure/ShopeeClone.Infrastructure.csproj` (where `AppDbContext` lives). Add it with `dotnet add <csproj> package Microsoft.EntityFrameworkCore.Design` rather than working around it.
- A warning that the `dotnet-ef` global tool version is older than the project's EF Core runtime version is expected and non-fatal — do not try to "fix" it unless the command actually fails.
- If the connection fails with a SQL Server network error, check `ConnectionStrings:DefaultConnection` in `src/API/appsettings.json` — this machine uses a full local SQL Server instance (`Server=.`), not LocalDB.

## After running

Show the generated migration file path and the result of `dotnet ef database update`. If this migration was for a newly scaffolded entity, remind the user to verify the table/columns match what was intended (e.g. via `dotnet ef migrations script` or by inspecting the generated `Up()` method) before moving on.
