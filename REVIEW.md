# Revisión de la API — pendientes por urgencia

## 🔴 Alta (antes de exponer a un frontend real / prod)

- [ ] **`appsettings.Production.json` no existe** — `Cors:AllowedOrigins` en `appsettings.json` (base) está vacío a propósito (fail-closed), así que hoy CORS bloquea todo en cualquier deploy sin gatear por entorno. Cuando exista el dominio real del frontend (Angular o React, aún sin decidir), crear `appsettings.Production.json` con ese origen, o setear la variable de entorno `Cors__AllowedOrigins__0=https://tudominio.com` en el servidor — nunca commitear el dominio real directo en `appsettings.json`.
- [ ] **Credenciales en texto plano committeadas** — `API/appsettings.Development.json` está trackeado en git con la misma password de `docker-compose.yml` (`P4ssw0rd@1`). Es SQL local de dev, pero queda en el historial del repo. Mover a `dotnet user-secrets`/variable de entorno antes de que el repo tenga más historia o se haga público.

## 🟡 Media (mejoras antes de crecer con más recursos/auth)

- [ ] **`ErrorController.cs`** — `UseStatusCodePagesWithReExecute` dispara para cualquier status sin body (401, 403, 405...), pero solo mapea 404 y "todo lo demás → `ServerError`". Cuando agregues auth, sumar casos explícitos para 401/403.

## 🟢 Baja (housekeeping / no bloquea nada)

- [ ] **`JsonSerializerOptions` recreado por excepción** en `ExceptionMiddleware.cs:25` — moverlo a `static readonly`.
- [ ] **Sin tests** — ni unitarios ni de integración todavía.
- [ ] **`GetBrands`/`GetTypes` sin cache** — pegan a la DB (`SELECT DISTINCT`) en cada request; cambian poco, buen candidato a cachear en memoria si el catálogo escala.
- [ ] **Filtro `brand`/`type`/`search` depende de la collation de SQL Server** (`ProductRepository.cs`) — igualdad exacta (`brand`/`type`) y `Contains` (`search`) confían en que la DB use collation case-insensitive (confirmado hoy: `SQL_Latin1_General_CP1_CI_AS`), sin normalización explícita en código. Si algún día cambia la collation de la DB, esto se rompe silenciosamente.

## ✅ Ya resuelto

- [x] **DTOs en todos los endpoints de Product** — `CreateProductDto`/`UpdateProductDto` (entrada) + `ProductDto` (salida) en `API/Dtos/Products/`, mapeo con `ProductMappings.ToDto()`. `ProductController` ya no bindea ni devuelve la entidad `Product` en ninguna dirección.
- [x] **Falsos positivos de `IDE0005` en VS Code** — `GenerateDocumentationFile` + `NoWarn CS1591` en `API/API.csproj`.
- [x] **Validación de datos en `Product`** — DataAnnotations (`[Required]`/`[MaxLength]`/`[Range]`/`[Url]`) en `CreateProductDto`; `UpdateProductDto` hereda de él + valida `Id`. Se puso en los DTOs de `API`, no en la entidad de `Core` (el dominio no depende de reglas de presentación). El `400` sale por la `InvalidModelStateResponseFactory` ya existente.
- [x] Shadowing de `ProblemDetails.Status/Title` en `ApiErrorResponse`.
- [x] Ruta hardcodeada del seed (`StoreContextSeed.cs`) — ahora usa `AppContext.BaseDirectory` + `CopyToOutputDirectory`.
- [x] Auto-migrate/seed corría en cualquier entorno sin control — ahora gateado a `Development`, con `ILogger` en vez de `Console.WriteLine`.
- [x] `WeatherForecastController.cs` / `WeatherForecast.cs` (leftover del template) eliminados.
- [x] **CORS restringido por entorno** — `Program.cs` lee `Cors:AllowedOrigins` de config y usa `WithOrigins(...)` en vez de `AllowAnyOrigin()`. Dev cubre `localhost:4200`/`:5173` en http y https (Angular/React aún sin decidir); base/prod queda vacío a propósito (ver pendiente "Alta" arriba: falta `appsettings.Production.json`).
- [x] **Paginación en `GetProducts`** — `pageIndex`/`pageSize` (default 1/6, tope `MaxPageSize=50`) resueltos en `ProductRepository` con `Skip`/`Take` antes de materializar la query; respuesta envuelta en `Pagination<T>` (`API/Dtos/Pagination.cs`). Ver README sección 13.
