# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Skinet — e-commerce backend in .NET 10 (Angular frontend planned but not yet started). Currently only a `Product` CRUD exists; this is an early-stage project, not a full storefront yet.

## Commands

```bash
# Database (SQL Server via Azure SQL Edge, Apple Silicon compatible)
docker compose up -d

# Run the API (auto-applies migrations + seeds data, but only when ASPNETCORE_ENVIRONMENT=Development)
dotnet run --project API

# Build
dotnet build

# Migrations (dotnet-ef must be installed globally: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Name> -p Infrastructure -s API
dotnet ef database update -p Infrastructure -s API
```

No test project exists yet.

API runs at `https://localhost:7075` / `http://localhost:5063` (Development). Interactive docs at `/scalar/v1` (Scalar, not Swagger UI).

## Architecture

Three projects, dependencies flow one way: `API → Infrastructure → Core`.

- **Core** — domain entities only (`Product`, `BaseEntity`) and repository interfaces (`IProductRepository`). No external dependencies, no EF references.
- **Infrastructure** — EF Core: `StoreContext` (DbContext), `IEntityTypeConfiguration<T>` classes under `Config/`, repository implementations, migrations, and `StoreContextSeed` (reads `Data/SeedData/products.json`, copied to output via `CopyToOutputDirectory` in the csproj — always resolve seed file paths with `AppContext.BaseDirectory`, never a relative path from CWD).
- **API** — controllers, error handling, DI wiring, HTTP pipeline (`Program.cs`).

`API` never references `Core` directly — only through `Infrastructure`.

### Response contract (important — apply consistently to every new controller)

All 2xx and 4xx/5xx responses follow a standard shape, not ASP.NET's defaults:

- **`API/Errors/ApiResponse<T>.cs`** — wraps every success response: `{ statusCode, message, data }`. Built via `BaseApiController.ApiOk(data, message?)` (200) and `ApiCreated(data, actionName, routeValues, message?)` (201).
- **`API/Errors/ApiErrorResponse.cs`** — extends `ProblemDetails` (not shadowed — `Status`/`Title` are the inherited properties, assigned in the constructor). Adds `ErrorCode` (stable string for the consumer to branch on, e.g. `"PRODUCT_UPDATE_ERROR"`) and `Details` (exception detail, populated only when `env.IsDevelopment()`). Built via `BaseApiController.ApiError(statusCode, message?, errorCode?)`.
- **`API/Errors/ErrorCodes/`** — partial `ErrorCodes` class, one file per resource (`ErrorCodes.Product.cs`, `ErrorCodes.Validation.cs`, `ErrorCodes.General.cs`). When adding a new controller/resource, add its own `ErrorCodes.<Resource>.cs` nested class following the same pattern rather than dumping everything in one file.
- **`API/Middleware/ExceptionMiddleware.cs`** — global catch-all for unhandled exceptions → uniform 500 `ApiErrorResponse`; stack trace only leaks in `Development`.
- **`API/Errors/ErrorController.cs`** + `app.UseStatusCodePagesWithReExecute("/errors/{0}")` in `Program.cs` — catches status codes with no body (e.g. 404 on an unmatched route) and re-executes them into a proper `ApiErrorResponse`.
- **`Program.cs`**'s `ApiBehaviorOptions.InvalidModelStateResponseFactory` — overrides the automatic `[ApiController]` 400 response for model binding/validation failures so it also uses `ApiErrorResponse` rather than leaking raw deserialization messages (which otherwise include fully-qualified CLR type names) outside Development.

Any new controller should inherit `BaseApiController` and use `ApiOk`/`ApiCreated`/`ApiError` rather than `ControllerBase`'s raw `Ok()`/`NotFound()`/`BadRequest()`, to keep the response contract consistent.

### Known gaps (tracked in `REVIEW.md`)

`REVIEW.md` at the repo root lists open issues found during review, ranked by urgency (CORS not yet restricted per-environment, missing DTOs on `Product` endpoints, etc). Check it before assuming those areas are settled.

### `README.md`

Updated per finished feature/section, with the reasoning and commands used at the time. Treat it as a narrative log, not always the latest code — a section can describe an implementation that was later refactored (e.g. its "Controladores" section predates the response-contract rework above). For current behavior, trust this file and the code; use the README for the "why" behind a past decision.
