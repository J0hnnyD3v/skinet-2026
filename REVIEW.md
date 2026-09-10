# Revisión de la API — pendientes por urgencia

## 🔴 Alta (antes de exponer a un frontend real / prod)

- [ ] **CORS sin gate de entorno** — `API/Program.cs` usa `AllowAnyOrigin/AnyMethod/AnyHeader` sin condicionar por `IsDevelopment()`. Restringir a los dominios reales del frontend antes de prod (`WithOrigins(...)`).
- [ ] **Credenciales en texto plano committeadas** — `API/appsettings.Development.json` está trackeado en git con la misma password de `docker-compose.yml` (`P4ssw0rd@1`). Es SQL local de dev, pero queda en el historial del repo. Mover a `dotnet user-secrets`/variable de entorno antes de que el repo tenga más historia o se haga público.

## 🟡 Media (mejoras antes de crecer con más recursos/auth)

- [ ] **`ErrorController.cs`** — `UseStatusCodePagesWithReExecute` dispara para cualquier status sin body (401, 403, 405...), pero solo mapea 404 y "todo lo demás → `ServerError`". Cuando agregues auth, sumar casos explícitos para 401/403.
- [ ] **Validación de datos en `Product`** — sin `[Range]`/reglas en `Price`, `QuantityInStock`, etc. (Core/Entities/Product.cs).
- [ ] **Sin paginación en `GetProducts`** — ya tiene filtrado (`brand`/`type`) y orden (`sort`), pero sin `pageIndex`/`pageSize`. No urge con pocos productos, pero crece mal si el catálogo aumenta.

## 🟢 Baja (housekeeping / no bloquea nada)

- [ ] **`JsonSerializerOptions` recreado por excepción** en `ExceptionMiddleware.cs:25` — moverlo a `static readonly`.
- [ ] **Sin tests** — ni unitarios ni de integración todavía.
- [ ] **`GetBrands`/`GetTypes` sin cache** — pegan a la DB (`SELECT DISTINCT`) en cada request; cambian poco, buen candidato a cachear en memoria si el catálogo escala.
- [ ] **Filtro `brand`/`type` es igualdad exacta** (`ProductRepository.cs`) — depende de la collation de SQL Server para case-insensitivity, sin normalización explícita en código.

## ✅ Ya resuelto

- [x] **DTOs para Create/Update Product** — `CreateProductDto`/`UpdateProductDto` en `API/Dtos/Product/`, `ProductController` ya no bindea la entidad directo del body.
- [x] Shadowing de `ProblemDetails.Status/Title` en `ApiErrorResponse`.
- [x] Ruta hardcodeada del seed (`StoreContextSeed.cs`) — ahora usa `AppContext.BaseDirectory` + `CopyToOutputDirectory`.
- [x] Auto-migrate/seed corría en cualquier entorno sin control — ahora gateado a `Development`, con `ILogger` en vez de `Console.WriteLine`.
- [x] `WeatherForecastController.cs` / `WeatherForecast.cs` (leftover del template) eliminados.
- [x] CORS agregado (falta el punto de "Alta" de arriba: restringir en prod).
