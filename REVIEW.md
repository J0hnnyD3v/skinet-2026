# Revisión de la API — pendientes por urgencia

## 🔴 Alta (antes de exponer a un frontend real / prod)

- [ ] **CORS sin gate de entorno** — `API/Program.cs` usa `AllowAnyOrigin/AnyMethod/AnyHeader` sin condicionar por `IsDevelopment()`. Restringir a los dominios reales del frontend antes de prod (`WithOrigins(...)`).
- [ ] **Credenciales en texto plano committeadas** — `API/appsettings.Development.json` está trackeado en git con la misma password de `docker-compose.yml` (`P4ssw0rd@1`). Es SQL local de dev, pero queda en el historial del repo. Mover a `dotnet user-secrets`/variable de entorno antes de que el repo tenga más historia o se haga público.

## 🟡 Media (mejoras antes de crecer con más recursos/auth)

- [ ] **`ErrorController.cs`** — `UseStatusCodePagesWithReExecute` dispara para cualquier status sin body (401, 403, 405...), pero solo mapea 404 y "todo lo demás → `ServerError`". Cuando agregues auth, sumar casos explícitos para 401/403.

## 🟢 Baja (housekeeping / no bloquea nada)

- [ ] **`JsonSerializerOptions` recreado por excepción** en `ExceptionMiddleware.cs:25` — moverlo a `static readonly`.
- [ ] **Sin tests** — ni unitarios ni de integración todavía.
- [ ] **`GetBrands`/`GetTypes` sin cache** — pegan a la DB (`SELECT DISTINCT`) en cada request; cambian poco, buen candidato a cachear en memoria si el catálogo escala.
- [ ] **Filtro `brand`/`type` es igualdad exacta** (`ProductRepository.cs`) — depende de la collation de SQL Server para case-insensitivity, sin normalización explícita en código.

## ✅ Ya resuelto

- [x] **DTOs en todos los endpoints de Product** — `CreateProductDto`/`UpdateProductDto` (entrada) + `ProductDto` (salida) en `API/Dtos/Products/`, mapeo con `ProductMappings.ToDto()`. `ProductController` ya no bindea ni devuelve la entidad `Product` en ninguna dirección.
- [x] **Falsos positivos de `IDE0005` en VS Code** — `GenerateDocumentationFile` + `NoWarn CS1591` en `API/API.csproj`.
- [x] **Validación de datos en `Product`** — DataAnnotations (`[Required]`/`[MaxLength]`/`[Range]`/`[Url]`) en `CreateProductDto`; `UpdateProductDto` hereda de él + valida `Id`. Se puso en los DTOs de `API`, no en la entidad de `Core` (el dominio no depende de reglas de presentación). El `400` sale por la `InvalidModelStateResponseFactory` ya existente.
- [x] Shadowing de `ProblemDetails.Status/Title` en `ApiErrorResponse`.
- [x] Ruta hardcodeada del seed (`StoreContextSeed.cs`) — ahora usa `AppContext.BaseDirectory` + `CopyToOutputDirectory`.
- [x] Auto-migrate/seed corría en cualquier entorno sin control — ahora gateado a `Development`, con `ILogger` en vez de `Console.WriteLine`.
- [x] `WeatherForecastController.cs` / `WeatherForecast.cs` (leftover del template) eliminados.
- [x] CORS agregado (falta el punto de "Alta" de arriba: restringir en prod).
- [x] **Paginación en `GetProducts`** — `pageIndex`/`pageSize` (default 1/6, tope `MaxPageSize=50`) resueltos en `ProductRepository` con `Skip`/`Take` antes de materializar la query; respuesta envuelta en `Pagination<T>` (`API/Dtos/Pagination.cs`). Ver README sección 13.
