# Revisión de la API — pendientes por urgencia

## 🔴 Alta (antes de exponer a un frontend real / prod)

- [ ] **`appsettings.Production.json` no existe** — `Cors:AllowedOrigins` en `appsettings.json` (base) está vacío a propósito (fail-closed), así que hoy CORS bloquea todo en cualquier deploy sin gatear por entorno. Cuando exista el dominio real del frontend (Angular o React, aún sin decidir), crear `appsettings.Production.json` con ese origen, o setear la variable de entorno `Cors__AllowedOrigins__0=https://tudominio.com` en el servidor — nunca commitear el dominio real directo en `appsettings.json`.

## 🟡 Media (mejoras antes de crecer con más recursos/auth)

- [ ] **`ErrorController.cs`** — `UseStatusCodePagesWithReExecute` dispara para cualquier status sin body (401, 403, 405...), pero solo mapea 404 y "todo lo demás → `ServerError`". Cuando agregues auth, sumar casos explícitos para 401/403.

## 🟢 Baja (housekeeping / no bloquea nada)

- [ ] **Sin tests** — ni unitarios ni de integración todavía.
- [ ] **`GetBrands`/`GetTypes` sin cache** — pegan a la DB (`SELECT DISTINCT`) en cada request; cambian poco, buen candidato a cachear en memoria si el catálogo escala.
- [ ] **Filtro `brand`/`type`/`search` depende de la collation de SQL Server** (`ProductRepository.cs`) — igualdad exacta (`brand`/`type`) y `Contains` (`search`) confían en que la DB use collation case-insensitive (confirmado hoy: `SQL_Latin1_General_CP1_CI_AS`), sin normalización explícita en código. Si algún día cambia la collation de la DB, esto se rompe silenciosamente.

## ⚠️ Riesgos aceptados (expuestos, no resueltos — decisión consciente de no actuar por ahora)

- [x] **Password de SA sigue en el historial de git** — commits viejos de `appsettings.Development.json`/`docker-compose.yml` exponen `P4ssw0rd@1` vía `git log -p` (repo público en GitHub: `J0hnnyD3v/skinet-2026`). **Sigue expuesta** — no se reescribió la historia ni se rotó la password. Razones para aceptar el riesgo, no para ignorarlo:
  - El SQL Server solo escucha en `localhost:1433` dentro de Docker — no es un endpoint expuesto a internet, así que conocer la password no le sirve a nadie fuera de la máquina de dev.
  - Es una base 100% descartable: `dotnet run` en Development re-aplica migraciones y re-siembra los datos desde cero (`StoreContextSeed`), no hay nada real que proteger ahí.
  - Reescribir historia (`git filter-repo`/BFG) cambiaría el hash de los 9 commits del repo y requeriría force-push sobre un repo público — costo/riesgo desproporcionado para un secreto sin explotabilidad real.
  - **Revisar de nuevo si:** el proyecto pasa a tener datos reales, el SQL Server se expone más allá de `localhost`, o el repo empieza a tener colaboradores/forks activos.

## ✅ Ya resuelto

- [x] **DTOs en todos los endpoints de Product** — `CreateProductDto`/`UpdateProductDto` (entrada) + `ProductDto` (salida) en `API/Dtos/Products/`, mapeo con `ProductMappings.ToDto()`. `ProductController` ya no bindea ni devuelve la entidad `Product` en ninguna dirección.
- [x] **Falsos positivos de `IDE0005` en VS Code** — `GenerateDocumentationFile` + `NoWarn CS1591` en `API/API.csproj`.
- [x] **Validación de datos en `Product`** — DataAnnotations (`[Required]`/`[MaxLength]`/`[Range]`/`[Url]`) en `CreateProductDto`; `UpdateProductDto` hereda de él + valida `Id`. Se puso en los DTOs de `API`, no en la entidad de `Core` (el dominio no depende de reglas de presentación). El `400` sale por la `InvalidModelStateResponseFactory` ya existente.
- [x] Shadowing de `ProblemDetails.Status/Title` en `ApiErrorResponse`.
- [x] Ruta hardcodeada del seed (`StoreContextSeed.cs`) — ahora usa `AppContext.BaseDirectory` + `CopyToOutputDirectory`.
- [x] Auto-migrate/seed corría en cualquier entorno sin control — ahora gateado a `Development`, con `ILogger` en vez de `Console.WriteLine`.
- [x] `WeatherForecastController.cs` / `WeatherForecast.cs` (leftover del template) eliminados.
- [x] **CORS restringido por entorno** — `Program.cs` lee `Cors:AllowedOrigins` de config y usa `WithOrigins(...)` en vez de `AllowAnyOrigin()`. Dev cubre `localhost:4200`/`:5173` en http y https (Angular/React aún sin decidir); base/prod queda vacío a propósito (ver pendiente "Alta" arriba: falta `appsettings.Production.json`).
- [x] **Credenciales fuera de archivos trackeados** — connection string movida a `dotnet user-secrets` (`API.csproj` solo tiene el `UserSecretsId`, sin secretos); `MSSQL_SA_PASSWORD` de `docker-compose.yml` movida a `.env` (gitignorado) + `.env.example` con placeholder para quien clone el repo.
- [x] **Paginación en `GetProducts`** — `pageIndex`/`pageSize` (default 1/6, tope `MaxPageSize=50`) resueltos en `ProductRepository` con `Skip`/`Take` antes de materializar la query; respuesta envuelta en `Pagination<T>` (`API/Dtos/Pagination.cs`). Ver README sección 13.
- [x] **`JsonSerializerOptions` recreado por excepción** en `ExceptionMiddleware.cs` — ahora es `static readonly JsonOptions`, se crea una sola vez en vez de en cada excepción.
