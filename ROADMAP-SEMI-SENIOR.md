# Roadmap: Desarrollador .NET Semi Senior (Middle) — APIs con C#

> Guía realista de lo que se espera de un dev semi senior en .NET enfocado en backend/APIs REST, tomando como referencia proyectos tipo **Skinet** (ASP.NET Core + EF Core + SQL Server, arquitectura en capas).
>
> Versión de referencia: **.NET 10 (LTS) / C# 14**. La mayoría del contenido aplica igual a .NET 8/9.

---

## Índice

1. [¿Qué significa realmente ser semi senior?](#1-qué-significa-realmente-ser-semi-senior)
2. [C# — el lenguaje](#2-c--el-lenguaje)
3. [Runtime y ecosistema .NET](#3-runtime-y-ecosistema-net)
4. [ASP.NET Core para APIs](#4-aspnet-core-para-apis)
5. [Diseño de APIs REST](#5-diseño-de-apis-rest)
6. [Acceso a datos: EF Core y SQL](#6-acceso-a-datos-ef-core-y-sql)
7. [Arquitectura](#7-arquitectura)
8. [Patrones de diseño (y cuándo NO usarlos)](#8-patrones-de-diseño-y-cuándo-no-usarlos)
9. [Código limpio y principios](#9-código-limpio-y-principios)
10. [Testing](#10-testing)
11. [Seguridad](#11-seguridad)
12. [Rendimiento, caché y resiliencia](#12-rendimiento-caché-y-resiliencia)
13. [Observabilidad](#13-observabilidad)
14. [DevOps, Git y entrega](#14-devops-git-y-entrega)
15. [Habilidades no técnicas](#15-habilidades-no-técnicas)
16. [Lo que NO se espera de un semi senior](#16-lo-que-no-se-espera-de-un-semi-senior)
17. [Checklist aplicado a Skinet](#17-checklist-aplicado-a-skinet)
18. [Plan de estudio sugerido](#18-plan-de-estudio-sugerido)
19. [Preguntas típicas de entrevista](#19-preguntas-típicas-de-entrevista)
20. [Recursos](#20-recursos)

---

## 1. ¿Qué significa realmente ser semi senior?

No es una lista de tecnologías, es un **nivel de autonomía**. Una forma honesta de verlo:

| | Junior | **Semi senior** | Senior |
|---|---|---|---|
| Autonomía | Necesita tareas bien definidas y guía constante | **Toma una historia/feature y la entrega de punta a punta con poca supervisión** | Define qué se construye y cómo, desbloquea a otros |
| Alcance | Una función, un endpoint | **Un feature completo o un módulo** | Un sistema o varios equipos |
| Decisiones técnicas | Sigue lo que ya existe | **Elige entre opciones conocidas y justifica el porqué** | Diseña la arquitectura y asume los trade-offs a largo plazo |
| Bugs | Los arregla con ayuda | **Los diagnostica solo (logs, debugger, SQL) y encuentra la causa raíz** | Previene clases enteras de bugs |
| Código de otros | Lo lee con dificultad | **Hace code reviews útiles** | Establece los estándares del equipo |
| Comunicación | Reporta avances | **Levanta riesgos temprano, estima con margen razonable** | Negocia alcance con negocio |

**En la práctica, un semi senior:**

- Entrega features en producción sin que alguien tenga que reescribir su código.
- Sabe *por qué* existen las convenciones del proyecto, no solo las copia.
- Sabe decir "esto no lo sé, pero sé cómo averiguarlo".
- Evita la sobreingeniería tanto como el código espagueti.
- Suele tener **2–4 años** de experiencia real (no es regla; hay gente que llega antes y gente que nunca llega).

---

## 2. C# — el lenguaje

### 2.1 Fundamentos que deben ser sólidos (sin dudar)

- **Tipos por valor vs. por referencia**: `struct` vs `class`, stack vs heap (a nivel conceptual), boxing/unboxing.
- **`record` y `record struct`**: igualdad por valor, `with`, cuándo usarlos (DTOs, value objects).
- **Nullable reference types** (`<Nullable>enable</Nullable>`): `?`, `!`, `required`, atributos como `[NotNullWhen]`. Nunca silenciar warnings con `!` por costumbre.
- **Inmutabilidad**: `init`, `readonly`, colecciones de solo lectura (`IReadOnlyList<T>`).
- **Interfaces vs clases abstractas**, métodos default en interfaces, `sealed`.
- **Generics**: restricciones (`where T : BaseEntity`), covarianza/contravarianza básica (`IEnumerable<out T>`).
- **Delegates, `Func<>`, `Action<>`, lambdas, eventos**.
- **Excepciones**: jerarquía, `throw;` vs `throw ex;` (preservar stack trace), filtros `catch (X) when (...)`, no usar excepciones para control de flujo normal.
- **`IDisposable` / `IAsyncDisposable`** y `using` / `await using`.

### 2.2 LINQ

- Diferencia entre **`IEnumerable<T>`** (en memoria) e **`IQueryable<T>`** (se traduce a SQL). Es la fuente #1 de problemas de rendimiento con EF Core.
- **Ejecución diferida**: la query no corre hasta que se enumera (`ToList`, `foreach`, `Count`...).
- Enumeración múltiple accidental.
- Operadores comunes: `Where`, `Select`, `SelectMany`, `GroupBy`, `Join`, `Any` vs `Count() > 0`, `First` vs `FirstOrDefault` vs `Single`.

### 2.3 Async/await (obligatorio dominarlo)

- Qué hace realmente `async/await` (no crea hilos; libera el hilo mientras espera I/O).
- **Nunca** `.Result` o `.Wait()` en código ASP.NET (riesgo de deadlocks y thread pool starvation).
- Evitar `async void` (salvo event handlers).
- **`CancellationToken`**: propagarlo desde el controller hasta EF Core (`ToListAsync(ct)`).
- `Task.WhenAll` para trabajo concurrente independiente.
- `ValueTask` — saber que existe y cuándo tiene sentido (rara vez en código de aplicación).
- `ConfigureAwait(false)`: no hace falta en ASP.NET Core (no hay `SynchronizationContext`), sí en librerías.

### 2.4 C# moderno (deberías reconocerlo y usarlo con criterio)

- Pattern matching: `is`, `switch` expressions, property/list patterns.
- Primary constructors (C# 12) — ojo: sus parámetros no son `readonly`.
- Collection expressions: `int[] x = [1, 2, 3];`
- File-scoped namespaces, global usings, `required` members.
- C# 14: palabra clave `field` en propiedades, extension members (`extension` blocks), asignación null-condicional (`a?.B = c`).

> **Criterio semi senior:** usar sintaxis moderna cuando mejora legibilidad, no para lucirse.

---

## 3. Runtime y ecosistema .NET

- **SDK vs Runtime**, `global.json`, TFM (`net10.0`).
- **Ciclo de soporte**: versiones pares son LTS (3 años), impares STS (2 años desde .NET 9). Saber justificar cuándo migrar.
- **NuGet**: versionado semántico, dependencias transitivas, *Central Package Management* (`Directory.Packages.props`), revisar vulnerabilidades (`dotnet list package --vulnerable`).
- **Estructura de soluciones**: `.sln`/`.slnx`, `Directory.Build.props` para configuración compartida (nullable, warnings as errors, analyzers).
- **CLI**: `dotnet build/run/test/publish/watch`, `dotnet ef`, `dotnet user-secrets`.
- **Garbage Collector** a nivel conceptual: generaciones, por qué muchas asignaciones pequeñas afectan rendimiento, LOH.
- **Licencias**: saber que varias librerías populares pasaron a modelo comercial (MediatR, AutoMapper, MassTransit, FluentAssertions v8+). Un semi senior revisa la licencia antes de agregar una dependencia.

---

## 4. ASP.NET Core para APIs

### 4.1 Hosting y pipeline

- `WebApplication.CreateBuilder`, `Program.cs`, qué va antes y después de `builder.Build()`.
- **Middleware**: el orden importa (`UseExceptionHandler` → `UseHttpsRedirection` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`). Saber escribir uno propio (como `ExceptionMiddleware` en Skinet).
- **Routing**: attribute routing, restricciones de ruta (`{id:int}`).

### 4.2 Inyección de dependencias (tema crítico)

- Lifetimes: **`Singleton`, `Scoped`, `Transient`** y qué pasa con cada uno por request.
- **Captive dependency**: inyectar un `Scoped` (ej. `DbContext`) dentro de un `Singleton` = bug. Saber detectarlo.
- `IServiceScopeFactory` para crear scopes en background services.
- Registrar por interfaz, extension methods para ordenar el registro (`services.AddApplicationServices()`).
- Keyed services (.NET 8+) — saber que existen.

### 4.3 Controllers vs Minimal APIs

- Dominar **al menos uno** y entender el otro.
- Controllers: `[ApiController]`, model binding (`[FromBody]`, `[FromQuery]`, `[FromRoute]`), `ActionResult<T>`, filtros.
- Minimal APIs: `MapGroup`, `TypedResults`, endpoint filters, validación integrada (.NET 10).
- Saber explicar trade-offs (Minimal: menos ceremonia, mejor rendimiento; Controllers: convenciones, filtros, familiaridad en equipos grandes).

### 4.4 Configuración

- `appsettings.json` + `appsettings.{Environment}.json` + variables de entorno + user-secrets (orden de precedencia).
- **Options pattern**: `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`, validación con `ValidateDataAnnotations().ValidateOnStart()`.
- **Nunca** secretos en el repo. En la nube: Azure Key Vault / AWS Secrets Manager.

### 4.5 Validación

- DataAnnotations (lo que usa Skinet hoy) y **FluentValidation** para reglas complejas o condicionales.
- Diferenciar: validación de **formato** (en el borde, DTOs) vs. **reglas de negocio** (dominio/servicios).

### 4.6 Manejo de errores

- Middleware global de excepciones o **`IExceptionHandler`** (.NET 8+) + `AddProblemDetails()`.
- **RFC 9457 (Problem Details)** — Skinet ya lo extiende con `ApiErrorResponse`.
- No filtrar stack traces fuera de Development.
- Distinguir errores esperados (404, 409, 422) de inesperados (500). Conocer el *Result pattern* como alternativa a lanzar excepciones para errores de negocio.

### 4.7 Documentación

- OpenAPI integrado (`Microsoft.AspNetCore.OpenApi`) + Scalar o Swagger UI.
- XML comments, `[ProducesResponseType]`, ejemplos. La documentación es parte del contrato.

### 4.8 Otros que debes conocer

- **CORS** por entorno.
- **Rate limiting** integrado (`AddRateLimiter`).
- **Versionado de API** (`Asp.Versioning`).
- **Health checks** (`AddHealthChecks`, `/health`).
- **Background services**: `BackgroundService`, `IHostedService`.
- **HttpClient** correcto: `IHttpClientFactory` / typed clients (nunca `new HttpClient()` por request).

---

## 5. Diseño de APIs REST

- Recursos con sustantivos en plural: `/api/products/{id}`, no `/api/getProduct`.
- **Verbos HTTP** y su semántica: idempotencia de `GET/PUT/DELETE`, `POST` no idempotente, `PATCH` parcial.
- **Códigos de estado correctos**:
  - `200` OK, `201` Created (+ header `Location`), `204` No Content
  - `400` request mal formado, `401` no autenticado, `403` sin permiso, `404`, `409` conflicto, `422` regla de negocio
  - `500` error del servidor
- **DTOs siempre** en la frontera (nunca exponer entidades de EF). Separar DTO de entrada y de salida.
- **Paginación** (`pageIndex`/`pageSize` o cursor), filtrado, ordenamiento, búsqueda. Devolver metadata (`totalCount`).
- Contrato de respuesta **consistente** (Skinet: `ApiResponse<T>` / `ApiErrorResponse`).
- Versionado y compatibilidad hacia atrás: no romper clientes existentes.
- Concurrencia optimista (`ETag` / `rowversion`) para evitar *lost updates*.
- Idempotency keys en operaciones críticas (pagos, órdenes).

---

## 6. Acceso a datos: EF Core y SQL

### 6.1 SQL (no negociable)

EF Core no te exime de saber SQL. Un semi senior debe:

- Escribir `JOIN`s, `GROUP BY`, subconsultas, CTEs y *window functions* básicas.
- Entender **índices** (clustered/non-clustered, compuestos, cobertura) y leer un plan de ejecución a nivel básico.
- Saber qué son las **transacciones** y los niveles de aislamiento.
- Normalización (hasta 3FN) y cuándo desnormalizar.

### 6.2 EF Core

- `DbContext` (scoped), `DbSet<T>`, `IEntityTypeConfiguration<T>` (Fluent API > atributos en entidades).
- **Migraciones**: crear, revisar el SQL generado (`dotnet ef migrations script`), estrategia para producción (scripts idempotentes o *migration bundles*, **no** `Migrate()` al arrancar en prod).
- **Relaciones**: 1:1, 1:N, N:N, owned types, delete behaviors.
- **Carga de datos**: `Include`/`ThenInclude`, proyecciones con `Select` (preferidas), lazy loading (y por qué suele evitarse).
- **Problema N+1**: detectarlo en logs y resolverlo.
- **Change tracking**: `AsNoTracking()` para lecturas, estados de entidad.
- **Split queries** vs *cartesian explosion*.
- `ExecuteUpdateAsync` / `ExecuteDeleteAsync` para operaciones masivas.
- Concurrencia: `[Timestamp]`/`rowversion`, manejar `DbUpdateConcurrencyException`.
- Ver el SQL generado (`LogTo`, `ToQueryString()`).
- Conocer **Dapper** como alternativa para consultas de lectura complejas o de alto rendimiento.

---

## 7. Arquitectura

### 7.1 Lo que debes dominar

- **Arquitectura en capas / N-Layer** (lo que usa Skinet: `API → Infrastructure → Core`): regla de dependencias, qué va en cada capa.
- **Clean Architecture / Onion / Hexagonal** — entender la idea central: *el dominio no depende de la infraestructura*; la infraestructura implementa interfaces definidas hacia adentro.
- Separación de responsabilidades: controllers delgados, lógica en servicios/dominio, acceso a datos en repositorios o directamente con `DbContext`.

### 7.2 Lo que debes conocer (explicar, no necesariamente implementar solo)

- **Vertical Slice Architecture**: organizar por feature en vez de por capa técnica.
- **CQRS** (separar lecturas de escrituras) — la versión simple, sin buses ni bases separadas.
- **Modular monolith** vs **microservicios**: por qué la mayoría de los proyectos no necesitan microservicios.
- **DDD táctico** básico: entidades, value objects, agregados, eventos de dominio. Diferencia entre modelo *anémico* y *rico*.
- Mensajería asíncrona a nivel conceptual (colas, RabbitMQ / Azure Service Bus, patrón Outbox).

> **Criterio semi senior:** la arquitectura correcta es la más simple que resuelve el problema actual y permite cambiar después. Clean Architecture con 7 proyectos para un CRUD de 5 entidades es sobreingeniería.

---

## 8. Patrones de diseño (y cuándo NO usarlos)

No se trata de memorizar los 23 del GoF, sino de **reconocer el problema que resuelve cada uno**.

### 8.1 Patrones que vas a usar constantemente

| Patrón | Qué resuelve | Ejemplo en APIs .NET | Cuándo NO |
|---|---|---|---|
| **Dependency Injection** | Desacoplar creación de uso, testabilidad | Todo ASP.NET Core | — (siempre) |
| **Repository** | Abstraer el acceso a datos | `IProductRepository` en Skinet | Si solo envuelve `DbSet` 1:1 sin agregar nada; `DbContext` ya es un repository + UoW |
| **Unit of Work** | Confirmar varios cambios en una sola transacción | `DbContext.SaveChangesAsync()`, o `IUnitOfWork` sobre repositorios genéricos | Si ya usas `DbContext` directo |
| **Specification** | Encapsular criterios de consulta reutilizables | `ProductsWithFiltersSpec` (se usa más adelante en Skinet) | Consultas únicas y simples |
| **Options** | Configuración tipada | `IOptions<JwtSettings>` | — |
| **Middleware / Chain of Responsibility** | Procesar requests en cadena | `ExceptionMiddleware` | — |
| **DTO + Mapper** | Separar contrato externo del modelo interno | `ProductDto` + `ProductMappings.ToDto()` | — |
| **Result pattern** | Errores de negocio sin excepciones | `Result<T>` con `IsSuccess`/`Error` | Si el equipo ya usa excepciones de forma consistente y funciona |

### 8.2 Patrones GoF que debes reconocer y aplicar

- **Strategy** — algoritmos intercambiables (ej. métodos de pago, cálculo de envío). En .NET suele implementarse con DI + keyed services o un diccionario de estrategias.
- **Factory / Factory Method** — creación condicional de objetos.
- **Decorator** — agregar comportamiento sin modificar la clase (ej. `CachedProductRepository` que envuelve `ProductRepository`). Librería útil: Scrutor.
- **Adapter** — integrar APIs externas (pasarela de pago, servicio de correo) detrás de tu propia interfaz.
- **Builder** — construir objetos complejos paso a paso (muy usado en tests: *test data builders*).
- **Observer** — eventos de dominio, `INotification`.
- **Mediator** — desacoplar emisor y receptor (MediatR). **Ojo:** muchas veces agrega indirección sin beneficio; y MediatR ya es comercial.
- **Singleton** — entender que en .NET se hace vía DI (`AddSingleton`), no con la implementación clásica estática.
- **Template Method** — clase base con pasos personalizables (ej. `BaseApiController`).

### 8.3 Patrones de resiliencia/integración (conocer)

- **Retry, Circuit Breaker, Timeout** (Polly / `Microsoft.Extensions.Http.Resilience`).
- **Outbox** — publicar eventos de forma confiable junto con la transacción.
- **Cache-aside**.

### 8.4 Antipatrones que debes saber identificar

- **God class / God controller** — un controller con 30 acciones y lógica de negocio.
- **Anemic domain** cuando la lógica de negocio está regada por todos lados (no siempre es malo, pero hay que saberlo).
- **Service Locator** (`serviceProvider.GetService<T>()` en código de aplicación).
- **Generic repository sobre EF Core sin razón** — abstracción que oculta `IQueryable` y termina limitando.
- **Exponer entidades de EF en la API**.
- **Primitive obsession** — usar `string` para email, dinero, etc. donde un value object evita bugs.
- **Magic strings/numbers**.
- **Catch-all silencioso** — `catch (Exception) { }`.

---

## 9. Código limpio y principios

### 9.1 SOLID (explicarlo con ejemplos propios, no de memoria)

- **S** — Single Responsibility: una clase, una razón para cambiar. *Ej.: el controller no calcula precios ni arma queries.*
- **O** — Open/Closed: extender sin modificar. *Ej.: agregar un método de pago nuevo como otra `IPaymentStrategy`, sin tocar un `switch` gigante.*
- **L** — Liskov: una subclase no debe romper las expectativas de la base. *Ej.: un `ReadOnlyRepository` que lanza `NotSupportedException` en `Add` viola LSP.*
- **I** — Interface Segregation: interfaces pequeñas y específicas.
- **D** — Dependency Inversion: depender de abstracciones. *Ej.: `Core` define `IProductRepository`, `Infrastructure` lo implementa.*

### 9.2 Otros principios

- **DRY** — pero con cuidado: duplicación accidental ≠ duplicación de conocimiento. Mejor duplicar que acoplar dos cosas que evolucionan distinto.
- **KISS** y **YAGNI** — no construir para requisitos imaginarios.
- **Composición sobre herencia**.
- **Fail fast** — validar en la entrada, lanzar temprano (`ArgumentNullException.ThrowIfNull`).
- **Ley de Demeter** — evitar `order.Customer.Address.City.Name`.
- **Tell, don't ask** — `order.Cancel()` en vez de `if (order.Status == ...) order.Status = ...`.

### 9.3 Prácticas concretas en C#

- **Nombres** que revelan intención: `GetActiveProductsAsync`, no `GetData2`. Sufijo `Async` en métodos asíncronos.
- Convenciones de Microsoft: `PascalCase` para tipos/métodos/propiedades, `camelCase` para locales/parámetros, `_camelCase` para campos privados, `I` para interfaces.
- **Métodos cortos** con un nivel de abstracción; *guard clauses* y retornos tempranos en vez de `if` anidados.
- Pocos parámetros (si son muchos → objeto de parámetros).
- Comentarios explican el **porqué**, no el qué. El código explica el qué.
- Sin código muerto ni comentado.
- **Constantes/enums** en vez de magic strings (Skinet: `ErrorCodes`).
- Clases `sealed` por defecto si no están diseñadas para herencia.
- **`.editorconfig`** + analyzers (`AnalysisLevel`, `TreatWarningsAsErrors` en CI) + `dotnet format`.
- Boy Scout Rule: dejar el código un poco mejor de como lo encontraste (sin mezclar refactors grandes con features en el mismo PR).

### 9.4 Refactoring

- Reconocer *code smells*: métodos largos, clases grandes, parámetros de más, *feature envy*, *shotgun surgery*, condicionales repetidos.
- Refactorizaciones básicas con el IDE: extraer método/clase, renombrar, introducir parámetro, reemplazar condicional con polimorfismo.
- **Refactorizar con tests que te respalden**.

---

## 10. Testing

Esto es probablemente **lo que más separa a un junior de un semi senior** en entrevistas y en el día a día.

### 10.1 Qué debes saber hacer

- **Tests unitarios** con **xUnit** (o NUnit/MSTest): `[Fact]`, `[Theory]` + `[InlineData]`.
- Estructura **Arrange / Act / Assert**, un comportamiento por test, nombres descriptivos (`CreateProduct_WithNegativePrice_ReturnsBadRequest`).
- **Mocks/stubs** con **NSubstitute** o **Moq**. Mockear solo dependencias externas, no todo.
- Assertions: **Shouldly** o **AwesomeAssertions** (fork libre de FluentAssertions).
- **Tests de integración** con `WebApplicationFactory<Program>`: levantar la API en memoria y hacer requests HTTP reales.
- **Base de datos real en tests** con **Testcontainers** (SQL Server en Docker). EF Core InMemory **no** es buen sustituto (no se comporta como una DB relacional).
- Test data builders / **Bogus** para datos falsos.

### 10.2 Criterio

- **Pirámide de testing** (o *testing trophy*): muchos unitarios en lógica de negocio, integración en endpoints críticos, pocos E2E.
- Qué **no** vale la pena testear: getters/setters, código de framework, mapeos triviales.
- Cobertura como indicador, no como meta (80% de cobertura con tests inútiles no sirve).
- Tests deterministas: nada de `DateTime.Now` directo → usar `TimeProvider` (.NET 8+).
- Conocer TDD aunque no lo practiques siempre.

---

## 11. Seguridad

### 11.1 Autenticación y autorización

- Diferencia entre **autenticación** (quién eres) y **autorización** (qué puedes hacer).
- **JWT**: estructura (header.payload.signature), claims, expiración, refresh tokens, dónde **no** guardar tokens.
- **ASP.NET Core Identity** (con `MapIdentityApi` o manual).
- Nociones de **OAuth 2.0 / OpenID Connect** (flujos, proveedores externos como Entra ID, Auth0, Keycloak).
- Autorización basada en **roles**, **claims** y **policies** (`[Authorize(Policy = "...")]`), *resource-based authorization*.

### 11.2 OWASP Top 10 aplicado a APIs

- **Broken Object Level Authorization (BOLA/IDOR)**: que el usuario A no pueda ver `/api/orders/123` del usuario B. Es la vulnerabilidad #1 en APIs.
- **Mass assignment**: evitado con DTOs de entrada (Skinet ya lo hace).
- **Inyección SQL**: EF Core parametriza, pero cuidado con `FromSqlRaw` + concatenación.
- Exposición excesiva de datos (devolver campos que el cliente no necesita).
- Rate limiting contra abuso/fuerza bruta.
- HTTPS obligatorio, HSTS, CORS restringido.
- **Secretos**: user-secrets en local, vault en la nube, nunca en git. Si se filtró uno, rotarlo (borrarlo del historial no basta).
- Hashing de contraseñas (Identity lo hace; nunca inventes tu propio algoritmo).
- Dependencias vulnerables.
- Logs sin datos sensibles (contraseñas, tokens, datos de tarjetas).

---

## 12. Rendimiento, caché y resiliencia

- **Medir antes de optimizar**: BenchmarkDotNet para código, `dotnet-counters`/`dotnet-trace` para la app, k6 o JMeter para carga.
- Causas típicas de lentitud: N+1, falta de índices, traer columnas/filas de más, falta de paginación, bloqueos síncronos (`.Result`).
- **Caché**:
  - `IMemoryCache` (una instancia), **`IDistributedCache`** / **Redis** (varias instancias).
  - **`HybridCache`** (.NET 9+): combina memoria + distribuida y protege contra *cache stampede*.
  - **Output caching** a nivel de endpoint.
  - Estrategias de invalidación (lo difícil de la caché).
- **Compresión** de respuestas.
- **Resiliencia** en llamadas HTTP externas: retries con backoff, timeouts, circuit breaker (`AddStandardResilienceHandler`).
- Nociones de `Span<T>`, `ArrayPool`, `StringBuilder` — saber que existen; no son necesarios en la mayoría de APIs CRUD.

---

## 13. Observabilidad

- **Logging estructurado** con `ILogger<T>` y message templates (`_logger.LogInformation("Producto {ProductId} creado", id)`), **no** interpolación de strings.
- Niveles de log y cuándo usar cada uno.
- **Serilog** (sinks a consola, Seq, Elastic, etc.) o el logging nativo con OpenTelemetry.
- *Source-generated logging* (`[LoggerMessage]`) — conocerlo.
- **Correlation ID / trace ID** para seguir un request a través de servicios.
- **OpenTelemetry**: logs, métricas y trazas. Conocer **.NET Aspire** para desarrollo local y dashboard.
- Health checks para orquestadores (Kubernetes, App Service).
- Saber **diagnosticar un bug en producción** solo con logs y métricas.

---

## 14. DevOps, Git y entrega

### 14.1 Git

- Flujo de ramas (GitHub Flow o trunk-based; conocer Git Flow).
- `rebase` vs `merge`, resolver conflictos con confianza, `cherry-pick`, `stash`, `reflog` para recuperar trabajo.
- Commits pequeños y con mensajes claros (**Conventional Commits**: `feat:`, `fix:`, `refactor:`).
- Pull requests pequeños y enfocados, con descripción útil.

### 14.2 Contenedores

- Escribir un **Dockerfile multi-stage** para una API .NET (o usar `dotnet publish /t:PublishContainer`).
- **Docker Compose** para levantar API + DB + Redis en local (Skinet ya usa compose para SQL).
- Variables de entorno y configuración por entorno dentro de contenedores.

### 14.3 CI/CD

- Pipeline básico en **GitHub Actions** o Azure DevOps: restore → build → test → publish → deploy.
- Correr migraciones de forma controlada en el pipeline.
- Entornos (dev / staging / prod) y *feature flags* a nivel conceptual.

### 14.4 Nube (al menos una, a nivel práctico)

- **Azure** (lo más común con .NET): App Service o Container Apps, Azure SQL, Key Vault, Application Insights.
- O **AWS** equivalente: ECS/App Runner, RDS, Secrets Manager, CloudWatch.
- No necesitas certificaciones, pero sí haber desplegado algo tú mismo.

---

## 15. Habilidades no técnicas

Para semi senior pesan tanto como las técnicas:

- **Estimar** tareas con un margen razonable y avisar cuando la estimación se rompe.
- **Hacer preguntas** antes de construir lo equivocado; aclarar requisitos ambiguos.
- **Code review**: dar feedback específico, amable y justificado ("esto puede causar N+1 porque...") y recibirlo sin tomarlo personal.
- **Documentar** decisiones (README, ADRs cortos — como hace Skinet con `README.md` y `REVIEW.md`).
- **Leer código ajeno** y documentación oficial en inglés con fluidez.
- **Inglés** técnico: lectura fluida obligatoria; conversación intermedia abre muchísimas más oportunidades (trabajo remoto).
- Ayudar a juniors sin hacerles el trabajo.
- Entender el **negocio**: qué problema resuelve el feature, no solo el ticket.
- Priorizar: saber cuándo "suficientemente bueno" es la respuesta correcta.

---

## 16. Lo que NO se espera de un semi senior

Para no caer en el síndrome del impostor ni en la parálisis por estudiar todo:

- ❌ Diseñar arquitecturas distribuidas complejas desde cero.
- ❌ Dominar Kubernetes, Terraform o todos los servicios de la nube.
- ❌ Conocer internals del CLR, IL o el JIT a profundidad.
- ❌ Optimización de bajo nivel (`Span<T>`, `unsafe`, SIMD) en el día a día.
- ❌ Saber microservicios, event sourcing, sagas y Kafka en producción.
- ❌ Liderar equipos o definir la estrategia técnica.
- ❌ Saberse de memoria cada patrón GoF.
- ❌ Tener todas las respuestas; sí se espera saber **buscarlas bien y validar** (incluyendo lo que sugiere una IA).

---

## 17. Checklist aplicado a Skinet

Una forma práctica de medir tu avance: llevar este proyecto a un nivel "listo para producción". Si puedes implementar **y explicar el porqué** de cada punto, estás en nivel semi senior.

### Ya hecho ✅

- [x] Arquitectura en capas con regla de dependencias (`API → Infrastructure → Core`)
- [x] EF Core con Fluent API (`IEntityTypeConfiguration`), migraciones y seed
- [x] Repository pattern (`IProductRepository`)
- [x] DTOs de entrada/salida y mapeo manual
- [x] Validación con DataAnnotations en DTOs
- [x] Contrato de respuesta consistente (`ApiResponse<T>` / `ApiErrorResponse` basado en Problem Details)
- [x] Middleware global de excepciones sin filtrar stack trace fuera de Development
- [x] Error codes estables para el consumidor
- [x] Documentación OpenAPI + Scalar
- [x] Filtrado y ordenamiento

### Siguiente nivel 🎯

**Datos y API**
- [ ] Paginación con metadata (`pageIndex`, `pageSize`, `totalCount`)
- [ ] Specification pattern + repositorio genérico (y poder explicar sus desventajas)
- [ ] Unit of Work para operaciones que tocan varias entidades (carrito → orden)
- [ ] `CancellationToken` propagado de controller a EF Core
- [ ] `AsNoTracking` y proyecciones en consultas de lectura
- [ ] Concurrencia optimista (`rowversion`) en actualizaciones
- [ ] Nuevas entidades con relaciones (Order, OrderItem, DeliveryMethod, Address)

**Calidad**
- [ ] Proyecto de **tests unitarios** (xUnit + NSubstitute)
- [ ] Proyecto de **tests de integración** (`WebApplicationFactory` + Testcontainers)
- [ ] `.editorconfig` + analyzers + `TreatWarningsAsErrors`
- [ ] `Directory.Build.props` / Central Package Management
- [ ] FluentValidation para reglas complejas

**Seguridad**
- [ ] Autenticación con ASP.NET Core Identity + JWT o cookies
- [ ] Autorización por roles/policies (ej. solo `Admin` crea/edita productos)
- [ ] Protección contra IDOR en órdenes (un usuario solo ve las suyas)
- [ ] CORS restringido por entorno (pendiente en `REVIEW.md`)
- [ ] Secretos fuera del repo con user-secrets (pendiente en `REVIEW.md`)
- [ ] Rate limiting en endpoints sensibles (login)

**Rendimiento y resiliencia**
- [ ] **Redis** para el carrito de compras y caché de catálogo (`HybridCache`)
- [ ] Integración con pasarela de pago (Stripe) detrás de una interfaz (Adapter) con webhooks e idempotencia
- [ ] Resiliencia en llamadas HTTP externas

**Operación**
- [ ] Logging estructurado (Serilog u OpenTelemetry) con correlation ID
- [ ] Health checks (`/health` con chequeo de SQL y Redis)
- [ ] Dockerfile multi-stage de la API
- [ ] Pipeline de CI en GitHub Actions (build + tests)
- [ ] Deploy a Azure (o AWS) con migraciones controladas, sin `Migrate()` al arrancar en prod

---

## 18. Plan de estudio sugerido

Estimación realista con **~10 horas por semana** partiendo de nivel junior con bases de C#. Ajusta según tu experiencia.

| Fase | Duración | Enfoque | Entregable |
|---|---|---|---|
| **1. Bases firmes** | 4–6 semanas | C# moderno, LINQ, async/await, SQL (joins, índices), Git | Ejercicios + consultas SQL sobre la DB de Skinet |
| **2. API sólida** | 6–8 semanas | DI y lifetimes, EF Core a fondo, paginación, specification, validación, errores | Skinet con catálogo completo, paginado y bien modelado |
| **3. Testing** | 4 semanas | xUnit, mocks, `WebApplicationFactory`, Testcontainers | Suite de tests unitarios + integración corriendo en CI |
| **4. Seguridad** | 4 semanas | Identity, JWT, policies, OWASP API Top 10 | Login/registro, roles, órdenes protegidas |
| **5. Producción** | 4–6 semanas | Redis, pagos, logging, health checks, Docker, CI/CD, nube | Skinet desplegado y accesible públicamente |
| **6. Profundizar** | continuo | Clean Architecture, DDD táctico, CQRS simple, rendimiento, mensajería | Refactor de un módulo + ADR explicando la decisión |

**Consejos:**
- Construye y **despliega**; un proyecto desplegado con tests y CI vale más en tu portafolio que diez tutoriales completados.
- Después de cada feature, escribe en el README **por qué** lo hiciste así (ya lo haces — sigue así).
- Lee código de proyectos reales: [eShop](https://github.com/dotnet/eShop) de Microsoft, templates de Clean Architecture.
- Si ya trabajas: pide participar en code reviews y en la resolución de bugs de producción. Ahí se aprende más rápido que en cualquier curso.

---

## 19. Preguntas típicas de entrevista

Si puedes responder esto con seguridad **y con ejemplos de tu propio código**, estás listo:

**C# y .NET**
1. ¿Diferencia entre `IEnumerable<T>` e `IQueryable<T>`? ¿Qué pasa si llamas `.ToList()` antes del `.Where()`?
2. ¿Por qué no usar `.Result` en ASP.NET Core? ¿Qué es un deadlock/thread pool starvation?
3. ¿Diferencia entre `class`, `struct`, `record`?
4. ¿Qué es boxing?
5. ¿Qué hace `using` y cuándo implementas `IDisposable`?

**ASP.NET Core**
6. Explica `Singleton`, `Scoped` y `Transient`. ¿Qué pasa si inyectas un `DbContext` en un singleton?
7. ¿Cómo funciona el pipeline de middleware? ¿Por qué importa el orden?
8. ¿Cómo manejas errores globalmente?
9. ¿Cómo manejas configuración y secretos por entorno?
10. ¿Controllers o Minimal APIs? ¿Por qué?

**Datos**
11. ¿Qué es el problema N+1 y cómo lo resuelves en EF Core?
12. ¿Cuándo usar `AsNoTracking`?
13. ¿Cómo aplicas migraciones en producción?
14. ¿Qué es un índice y cuándo puede empeorar el rendimiento?
15. ¿Qué es concurrencia optimista?

**Diseño y arquitectura**
16. Explica SOLID con ejemplos de tu código.
17. ¿Tiene sentido un Repository sobre EF Core? Argumenta a favor y en contra.
18. ¿Qué es Clean Architecture y cuándo es sobreingeniería?
19. ¿Qué patrón usarías para soportar varios métodos de pago?
20. ¿Por qué no exponer entidades directamente en la API?

**Testing y seguridad**
21. ¿Diferencia entre test unitario y de integración? ¿Cómo testeas un endpoint con DB?
22. ¿Por qué EF Core InMemory no es buena opción para tests?
23. ¿Cómo funciona JWT? ¿Qué problemas tiene?
24. ¿Qué es IDOR/BOLA y cómo lo previenes?
25. ¿Cómo diagnosticarías un endpoint que se volvió lento en producción?

---

## 20. Recursos

**Oficiales (prioridad)**
- [Documentación de ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Documentación de EF Core](https://learn.microsoft.com/ef/core/)
- [Guía de C#](https://learn.microsoft.com/dotnet/csharp/)
- [.NET Architecture Guides](https://dotnet.microsoft.com/learn/dotnet/architecture-guides) (eBooks gratuitos: microservicios, arquitectura web moderna)
- [ASP.NET Core Guidance — David Fowler](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios) (async y errores comunes)
- [eShop (app de referencia)](https://github.com/dotnet/eShop)
- [OWASP API Security Top 10](https://owasp.org/API-Security/)

**Libros**
- *Clean Code* — Robert C. Martin (leer con criterio; algunas recomendaciones han envejecido)
- *Refactoring* (2ª ed.) — Martin Fowler
- *C# in Depth* — Jon Skeet
- *Unit Testing: Principles, Practices, and Patterns* — Vladimir Khorikov (muy recomendado)
- *Head First Design Patterns* — Freeman & Robson
- *Designing Data-Intensive Applications* — Martin Kleppmann (para la etapa de profundizar)

**Práctica**
- [refactoring.guru](https://refactoring.guru/es) — patrones de diseño y code smells, en español
- Blogs/canales: Andrew Lock, Nick Chapsas, Milan Jovanović, Steve "Ardalis" Smith, Khalid Abuhakmeh

---

> **Última idea:** el salto a semi senior no llega cuando terminas este documento, sino cuando **tomas decisiones técnicas y puedes defenderlas**: por qué este patrón y no otro, por qué este índice, por qué no hace falta caché todavía. Skinet es un buen campo de práctica para eso — cada entrada del `README.md` explicando un porqué es evidencia de ese criterio.
