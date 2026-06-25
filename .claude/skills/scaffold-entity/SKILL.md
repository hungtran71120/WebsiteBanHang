---
name: scaffold-entity
description: This skill should be used when the user asks to "scaffold an entity", "add a new entity", "create CRUD for X", "thêm entity mới", "scaffold entity Product/Category/Cart/Order/Review", "tạo CRUD cho ...", or wants to add a new domain object to the ShopeeClone project following its Clean Architecture pattern (Domain -> Application -> Infrastructure -> API).
---

Generate a new entity end-to-end for the ShopeeClone project (`E:\ProjectVuiVe\WebSiteBanHang`), following the exact Clean Architecture pattern already established in the codebase. Read `CLAUDE.md` section 3.1 for naming conventions before generating anything.

## Step 1 — Clarify before generating

Ask the user (do not guess) if any of the following is missing:
- Entity name (singular, PascalCase, e.g. `Product`, `Category`, `CartItem`).
- Fields with types (e.g. `Name: string`, `Price: decimal`, `Stock: int`, `CategoryId: Guid` for relations).
- Whether full CRUD is needed or the entity is read-mostly (e.g. `Review` typically only needs Create + List, not Update/Delete).
- Whether write operations (`Create`/`Update`/`Delete`) must be restricted to `Admin` role, per the project's `[Authorize(Roles = "Admin")]` convention used for admin-only management endpoints.

## Step 2 — Generate code in this order

1. **Domain** — create `src/Domain/Entities/{Name}.cs`. The class must inherit `BaseEntity` (see `src/Domain/Common/BaseEntity.cs` for the existing pattern: `Id`, `CreatedAt`, `UpdatedAt` already provided). Add only the requested properties; do not add speculative fields.

2. **Infrastructure**
   - Add `public DbSet<{Name}> {Name}s { get; set; }` to `src/Infrastructure/Persistence/AppDbContext.cs`.
   - Create `src/Infrastructure/Persistence/Configurations/{Name}Configuration.cs` implementing `IEntityTypeConfiguration<{Name}>`, configuring required fields, max lengths for strings, and relationships (foreign keys) as applicable. Apply configurations in `AppDbContext.OnModelCreating` via `ApplyConfigurationsFromAssembly` if not already wired — check first before adding a second mechanism.

3. **Application**
   - Create DTOs in `src/Application` following the naming convention from `CLAUDE.md`: `{Name}Dto`, `Create{Name}Request`, `Update{Name}Request` (only the requests actually needed per Step 1).
   - Create `I{Name}Service` interface and `{Name}Service` implementation covering only the use-cases confirmed in Step 1 (don't generate Update/Delete if the entity doesn't need them).
   - Register the service in `src/Application/DependencyInjection.cs` (`AddApplicationServices`).

4. **API**
   - Create `src/API/Controllers/{Name}sController.cs` using `I{Name}Service`, with standard REST routes (`GET /api/{name}s`, `GET /api/{name}s/{id}`, `POST`, `PUT`, `DELETE` as applicable).
   - Apply `[Authorize(Roles = "Admin")]` on write endpoints if confirmed in Step 1; leave read endpoints open or `[Authorize]` depending on whether the resource is public (e.g. product listing is public, order history requires auth).

## Step 3 — After generating

- Tell the user to run the `db-migration` skill to create and apply the EF Core migration for the new entity — do not run migrations automatically as part of this skill.
- Remind the user that the project's testing convention (`CLAUDE.md` section 2/5) requires unit tests for the new Application service (in `tests/Application.UnitTests`) and integration tests for the new controller endpoints (in `tests/API.IntegrationTests`). Offer to write them, but do not skip mentioning this requirement.
