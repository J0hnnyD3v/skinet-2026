# Skinet

Tienda e-commerce construida con .NET 10 (API) y Angular (cliente, en secciones posteriores).

---

## Sección: API Basics

Resumen de todo lo implementado en esta sección: creación de la solución, arquitectura por capas, Entity Framework Core, base de datos en Docker y un CRUD completo de `Product`.

### Objetivo

Levantar una Web API REST funcional con un endpoint de productos (GET / POST / PUT / DELETE) respaldado por SQL Server vía EF Core, siguiendo una arquitectura limpia en 3 proyectos.

---

### 1. Arquitectura de la solución

Tres proyectos con dependencias en una sola dirección:

```
API  ──►  Infrastructure  ──►  Core
```

| Proyecto | Tipo | Responsabilidad |
|---|---|---|
| **Core** | Class library | Entidades del dominio. Sin dependencias externas. |
| **Infrastructure** | Class library | Acceso a datos: `DbContext`, configuraciones de EF, migraciones. Referencia a `Core`. |
| **API** | ASP.NET Core Web API | Controladores, pipeline HTTP, inyección de dependencias, configuración. Referencia a `Infrastructure`. |

Reglas clave:

- `Core` no conoce a nadie. Es el centro.
- `API` nunca referencia directamente a `Core`; lo hace de forma transitiva a través de `Infrastructure`.
- Toda la lógica de EF Core vive en `Infrastructure`, no en `API`.

Comandos usados para crear la estructura:

```bash
dotnet new sln
dotnet new webapi -n API
dotnet new classlib -n Core
dotnet new classlib -n Infrastructure

dotnet sln add API Core Infrastructure

dotnet add Infrastructure reference Core
dotnet add API reference Infrastructure
```

Se eliminaron los `Class1.cs` autogenerados de `Core` e `Infrastructure`.

---

### 2. Entidades del dominio (Core)

**`BaseEntity`** — clase base con el `Id` compartido por todas las entidades:

```csharp
namespace Core.Entities;

public class BaseEntity
{
    public int Id { get; set; }
}
```

**`Product`** — entidad principal. Usa `required` para forzar que las propiedades string se asignen al construir el objeto (feature de C# 11):

```csharp
namespace Core.Entities;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string PictureUrl { get; set; }
    public required string Type { get; set; }
    public required string Brand { get; set; }
    public int QuantityInStock { get; set; }
}
```

---

### 3. Entity Framework Core (Infrastructure)

**`StoreContext`** — el `DbContext`. Usa constructor primario (C# 12) y carga automáticamente todas las configuraciones `IEntityTypeConfiguration` del assembly:

```csharp
public class StoreContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
    }
}
```

**`ProductConfiguration`** — configuración fluida por entidad. Se separa del contexto para mantenerlo limpio. Aquí se fija la precisión del `decimal` (si no, EF avisa con un warning y trunca):

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
    }
}
```

Paquetes NuGet:

- `Infrastructure`: `Microsoft.EntityFrameworkCore.SqlServer`
- `API`: `Microsoft.EntityFrameworkCore.Design` (necesario para el CLI de migraciones)

---

### 4. Base de datos en Docker

`docker-compose.yml` levanta SQL Server (Azure SQL Edge, compatible con Apple Silicon):

```yaml
services:
  sql:
    image: mcr.microsoft.com/azure-sql-edge:latest
    container_name: skinet-sql
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "P4ssw0rd@1"
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql

volumes:
  sql-data:
```

- El volumen `sql-data` persiste los datos entre reinicios del contenedor.
- Arrancar con: `docker compose up -d`

Connection string en `API/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=SkiNet;User Id=sa;Password=P4ssw0rd@1;TrustServerCertificate=True;"
}
```

`TrustServerCertificate=True` evita errores de certificado SSL autofirmado en local.

---

### 5. Configuración de la API (Program.cs)

```csharp
builder.Services.AddControllers();
builder.Services.AddDbContext<StoreContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI de documentación en /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

Aspectos clave:

- `AddDbContext` registra `StoreContext` con scope por request.
- La documentación se sirve con **Scalar** (`Scalar.AspNetCore`) en lugar de Swagger UI, consumiendo el documento OpenAPI nativo de .NET.
- El perfil de arranque (`launchSettings.json`) usa `https://localhost:7075` y `http://localhost:5063`, entorno `Development`.

---

### 6. Migraciones

```bash
# instalar la herramienta (una vez)
dotnet tool install --global dotnet-ef

# crear la migración inicial: proyecto con el DbContext + proyecto de arranque
dotnet ef migrations add InitialCreate -p Infrastructure -s API

# aplicar a la base de datos
dotnet ef database update -p Infrastructure -s API
```

`InitialCreate` genera la tabla `Products` con `Id` como identity/PK y `Price` como `decimal(18,2)` (tomado de `ProductConfiguration`).

> Nota: en esta sección la migración se aplica manualmente con `database update`. Aplicarla automáticamente al arrancar la app se ve más adelante.

---

### 7. Controladores

**`BaseApiController`** — base compartida con los atributos comunes. Todos los controladores heredan de aquí:

```csharp
[Route("api/[controller]")]
[ApiController]
public class BaseApiController : ControllerBase
{
}
```

- `[ApiController]` activa: validación automática de `ModelState` (respuesta `400 problem+json`), binding inferido, `[Required]` implícito en no-nulables.
- `[Route("api/[controller]")]` → la ruta base es `api/product` (el token `[controller]` quita el sufijo `Controller`).

**`ProductController`** — CRUD completo. Recibe `StoreContext` por inyección de dependencias:

```csharp
public class ProductController(StoreContext context) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        => await context.Products.ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProductById(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, Product product)
    {
        if (product.Id != id) return BadRequest("Product ID mismatch");
        if (!ProductExists(id)) return NotFound();

        context.Entry(product).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private bool ProductExists(int id) => context.Products.Any(x => x.Id == id);
}
```

Endpoints resultantes:

| Método | Ruta | Acción | Respuesta OK |
|---|---|---|---|
| GET | `/api/product` | Lista todos | `200` + array |
| GET | `/api/product/{id}` | Uno por id | `200` / `404` |
| POST | `/api/product` | Crea | `200` + producto |
| PUT | `/api/product/{id}` | Actualiza | `204` / `400` / `404` |
| DELETE | `/api/product/{id}` | Elimina | `204` / `404` |

---

### 8. Errores comunes vistos en esta sección

- **`405 Method Not Supported` en `PUT /api/product`** — la ruta del PUT es `{id:int}`, así que exige `/api/product/{id}`. Sin el id ningún endpoint PUT coincide y el routing responde 405.
- **`400 Bad Request` en `PUT /api/product/{id}`** — el body debe incluir `"id"` y coincidir con el id de la URL (`product.Id != id`). Conviene separar los chequeos para devolver `404` cuando el producto no existe en lugar de `400`.
- **Restricción de ruta `{id:int}`** — evita que llegue basura al binding; una ruta no numérica directamente no matchea.

---

### Herramientas usadas

| Herramienta | Uso |
|---|---|
| **.NET 10 SDK** | Framework de la API y CLI (`dotnet`) |
| **ASP.NET Core Web API** | Controladores REST |
| **Entity Framework Core 10** (`SqlServer`, `Design`) | ORM y migraciones |
| **dotnet-ef** | CLI de migraciones (`migrations add`, `database update`) |
| **Docker / docker compose** | Contenedor de SQL Server (`azure-sql-edge`) |
| **SQL Server (Azure SQL Edge)** | Base de datos |
| **OpenAPI + Scalar** (`Scalar.AspNetCore`) | Documentación interactiva de la API |
| **Bruno** (`SkinetBrunoCollection/`) | Cliente HTTP para probar los endpoints; usa `@faker-js/faker` para generar payloads |
| **C# 12** | Constructores primarios en `DbContext` y controladores |

---

### Cómo correr el proyecto

```bash
# 1. Base de datos
docker compose up -d

# 2. Migraciones (si es la primera vez)
dotnet ef database update -p Infrastructure -s API

# 3. API
dotnet run --project API

# 4. Documentación
#    https://localhost:7075/scalar/v1
```

---

## Sección: API Architecture

Resumen de todo lo implementado en esta sección: patrón repositorio, un contrato de respuesta estándar (éxito y error) reutilizable para toda la API, códigos de error propios, DTOs de entrada/salida en los endpoints de `Product` con validación por DataAnnotations, CORS, filtrado/orden de productos, y varios fixes de robustez sobre lo construido en API Basics.

### Objetivo

Dejar de devolver respuestas "genéricas" de ASP.NET (`Ok(x)`, `NotFound()`, `BadRequest("string")`) y establecer un contrato consistente: toda respuesta 2xx trae `statusCode` + `message` + `data`; toda respuesta 4xx/5xx extiende el estándar `ProblemDetails` de .NET con un código de error propio y, en desarrollo, detalle de la excepción.

---

### 1. Patrón repositorio (Core / Infrastructure)

Se extrajo el acceso a datos de `ProductController` (que antes recibía `StoreContext` directo) a una interfaz + implementación:

**`Core/Interfaces/IProductRepository.cs`**

```csharp
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? brand, string? type, string? sort);
    Task<Product?> GetProductByIdAsync(int id);
    Task<IReadOnlyList<string>> GetBrandsAsync();
    Task<IReadOnlyList<string>> GetTypesAsync();
    void AddProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
    bool ProductExists(int id);
    Task<bool> SaveChangesAsync();
}
```

**`Infrastructure/Data/ProductRepository.cs`** — implementación con EF Core, registrada en `Program.cs` con `AddScoped<IProductRepository, ProductRepository>()`. El controller ya no conoce `StoreContext` ni `DbSet`, solo la interfaz.

Motivación: `Core` define el contrato del acceso a datos sin depender de EF; `Infrastructure` es la única capa que sabe que existe SQL Server.

---

### 2. Filtrado y orden de productos (`GET /api/product`)

`GetProductsAsync` ahora acepta tres query params opcionales — `brand`, `type`, `sort` — y arma la query de forma incremental sobre `IQueryable<Product>` (nada se ejecuta contra la base hasta el `ToListAsync()` final, así que cada `Where`/`OrderBy` se traduce a SQL):

```csharp
public async Task<IReadOnlyList<Product>> GetProductsAsync(string? brand, string? type, string? sort)
{
    var query = context.Products.AsQueryable();

    if (!string.IsNullOrWhiteSpace(brand))
        query = query.Where(x => x.Brand == brand);

    if (!string.IsNullOrWhiteSpace(type))
        query = query.Where(x => x.Type == type);

    if (!string.IsNullOrWhiteSpace(sort))
    {
        query = sort switch
        {
            "priceAsc" => query.OrderBy(x => x.Price),
            "priceDesc" => query.OrderByDescending(x => x.Price),
            _ => query.OrderBy(x => x.Name)
        };
    }

    return await query.ToListAsync();
}
```

Ejemplo: `GET /api/product?brand=Nike&sort=priceDesc`.

Se agregaron además dos endpoints de soporte para poblar filtros en un futuro frontend:

| Método | Ruta | Devuelve |
|---|---|---|
| GET | `/api/product/brands` | Lista de `Brand` distintos |
| GET | `/api/product/types` | Lista de `Type` distintos |

---

### 3. Contrato de respuesta estándar (API/Errors)

#### Éxito — `ApiResponse<T>`

Envuelve toda respuesta 2xx con `statusCode`, `message` (con default según el código si no se especifica) y `data`:

```csharp
public class ApiResponse<T>(int statusCode, T? data = default, string? message = null)
{
    public int StatusCode { get; set; } = statusCode;
    public string Message { get; set; } = message ?? DefaultMessageFor(statusCode);
    public T? Data { get; set; } = data;
}
```

#### Error — `ApiErrorResponse`

Extiende `ProblemDetails` (el estándar RFC 7807 que ya usa .NET internamente) en vez de reemplazarlo — así cualquier cliente que solo entienda `ProblemDetails` sigue funcionando, y el que quiera más detalle lee los campos extra. Importante: `Status`/`Title` **no se shadowean** con `new`, se asignan en el constructor sobre las propiedades heredadas, para que el objeto se comporte igual sea tratado como `ApiErrorResponse` o como `ProblemDetails`:

```csharp
public class ApiErrorResponse : ProblemDetails
{
    public string? ErrorCode { get; set; }
    public string? Details { get; set; }  // solo se popula en Development

    public ApiErrorResponse(int statusCode, string? message = null, string? errorCode = null, string? details = null)
    {
        Status = statusCode;
        Title = message ?? DefaultMessageFor(statusCode);
        ErrorCode = errorCode;
        Details = details;
    }
}
```

#### `ErrorCode` — códigos únicos por causa

Cada error trae un `ErrorCode` estable (ej. `"PRODUCT_UPDATE_ERROR"`) para que el consumidor pueda ramificar lógica sin parsear el mensaje humano. Viven en `API/Errors/ErrorCodes/`, como una `partial class` dividida **un archivo por recurso** (pensando en que la API va a crecer con más entidades):

```
API/Errors/ErrorCodes/
  ErrorCodes.cs             -> declaración base (public static partial class ErrorCodes)
  ErrorCodes.Product.cs     -> PRODUCT_NOT_FOUND, PRODUCT_CREATE_ERROR, PRODUCT_UPDATE_ERROR, PRODUCT_DELETE_ERROR, PRODUCT_ID_MISMATCH
  ErrorCodes.Validation.cs  -> VALIDATION_ERROR
  ErrorCodes.General.cs     -> INTERNAL_SERVER_ERROR, RESOURCE_NOT_FOUND
```

Al agregar un recurso nuevo (`Order`, `Basket`, etc.) se suma un `ErrorCodes.<Recurso>.cs` más, mismo patrón.

#### Helpers en `BaseApiController`

Para no repetir el wrapping en cada acción de cada controller:

```csharp
protected ActionResult ApiOk<T>(T? data, string? message = null)
{
    if (data == null) return ApiError(StatusCodes.Status404NotFound, errorCode: ErrorCodes.General.NotFound);
    return Ok(new ApiResponse<T>(StatusCodes.Status200OK, data, message));
}

protected ActionResult ApiCreated<T>(T data, string actionName, object routeValues, string? message = null)
    => CreatedAtAction(actionName, routeValues, new ApiResponse<T>(StatusCodes.Status201Created, data, message));

protected ActionResult ApiError(int statusCode, string? message = null, string? errorCode = null)
    => StatusCode(statusCode, new ApiErrorResponse(statusCode, message, errorCode));
```

`ProductController` quedó así de simple, por ejemplo en el delete:

```csharp
[HttpDelete("{id:int}")]
public async Task<ActionResult> DeleteProduct(int id)
{
    var product = await repository.GetProductByIdAsync(id);
    if (product == null) return ApiError(StatusCodes.Status404NotFound, "Product not found", ErrorCodes.Product.NotFound);

    repository.DeleteProduct(product);

    if (await repository.SaveChangesAsync())
        return ApiOk(product, "Product deleted successfully");

    return ApiError(StatusCodes.Status400BadRequest, "Problem deleting the product", ErrorCodes.Product.DeleteError);
}
```

Se descartaron nombres como `HandleResult`/`HandleError` (muy planos, no dicen qué devuelven) a favor de `ApiOk`/`ApiCreated`/`ApiError`, que además no chocan con los métodos nativos de `ControllerBase` (`Ok`, `Created`).

---

### 4. Manejo global de errores no controlados — `ExceptionMiddleware`

Middleware que envuelve todo el pipeline y captura cualquier excepción no manejada (500), devolviendo el mismo `ApiErrorResponse` que el resto de la API. El stack trace solo se incluye en `Details` cuando `env.IsDevelopment()` — nunca en producción:

```csharp
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = env.IsDevelopment()
                ? new ApiErrorResponse(context.Response.StatusCode, ex.Message, ErrorCodes.General.ServerError, ex.StackTrace)
                : new ApiErrorResponse(context.Response.StatusCode, errorCode: ErrorCodes.General.ServerError);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, /* camelCase */));
        }
    }
}
```

Se registra primero en el pipeline (`app.UseMiddleware<ExceptionMiddleware>()`), antes que cualquier otro middleware, para capturar excepciones de todo lo que viene después.

---

### 5. 404 de rutas inexistentes — `ErrorController` + `UseStatusCodePagesWithReExecute`

Cuando no hay excepción pero tampoco hay body (ej. una ruta que no matchea ningún endpoint), ASP.NET responde con un 404 vacío por defecto. Se agregó:

```csharp
[ApiController]
[Route("errors/{code:int}")]
public class ErrorController : ControllerBase
{
    public ActionResult Error(int code)
    {
        var errorCode = code == StatusCodes.Status404NotFound
            ? ErrorCodes.General.NotFound
            : ErrorCodes.General.ServerError;

        return StatusCode(code, new ApiErrorResponse(code, errorCode: errorCode));
    }
}
```

```csharp
app.UseStatusCodePagesWithReExecute("/errors/{0}");
```

Así cualquier status code sin body se re-ejecuta contra este controller y sale con el mismo formato `ApiErrorResponse` que el resto de la API.

---

### 6. Validación automática de modelo (`[ApiController]`) sin filtrar detalles internos

Por defecto, cuando `[ApiController]` detecta un modelo inválido (falla de binding o de `required`), ASP.NET arma un 400 con el mensaje crudo del deserializador de `System.Text.Json`, que puede incluir el nombre completo del tipo CLR (ej. `"JSON deserialization for type 'Core.Entities.Product' was missing required properties..."`) — filtra detalles de implementación que no le importan al consumidor y que en producción no deberían exponerse.

Se sobreescribió la factory en `Program.cs`:

```csharp
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        var isDevelopment = context.HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>().IsDevelopment();

        var response = isDevelopment
            ? new ApiErrorResponse(StatusCodes.Status400BadRequest, "Validation failed", ErrorCodes.Validation.InvalidModel, string.Join(" | ", errors))
            : new ApiErrorResponse(StatusCodes.Status400BadRequest, "Validation failed", ErrorCodes.Validation.InvalidModel);

        return new BadRequestObjectResult(response);
    };
});
```

En desarrollo se ven los mensajes crudos en `Details`; en cualquier otro entorno, solo `"Validation failed"` + `ErrorCode`.

---

### 7. CORS

Agregado porque el frontend (Angular u otro, aún no decidido) va a correr en otro *origin* (puerto distinto) que la API, y el navegador bloquea esas requests sin política CORS configurada — esto no depende del framework que se elija:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
// ...
app.UseCors(CorsPolicy);
```

> Pendiente (ver `REVIEW.md`): esta política es permisiva a propósito para no bloquear el desarrollo mientras no hay frontend definido. Antes de producción hay que restringir `AllowAnyOrigin()` a los dominios reales.

---

### 8. Fixes de robustez sobre lo construido en API Basics

- **Migración + seed automáticos gateados a `Development`** (`Program.cs`): antes corrían en cualquier entorno al arrancar la app, lo cual es riesgoso en producción (migraciones destructivas sin control). Ahora:

  ```csharp
  if (app.Environment.IsDevelopment())
  {
      using var scope = app.Services.CreateScope();
      var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
      try
      {
          var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
          await context.Database.MigrateAsync();
          await StoreContextSeed.SeedAsync(context);
      }
      catch (Exception ex)
      {
          logger.LogError(ex, "An error occurred while migrating or seeding the database");
          throw;
      }
  }
  ```

  También se reemplazó `Console.WriteLine(ex)` por `ILogger`, consistente con el resto de la API.

- **Ruta del seed data ya no es relativa al working directory** (`StoreContextSeed.cs`): antes usaba `"../Infrastructure/Data/SeedData/products.json"`, que solo funcionaba si `dotnet run` se ejecutaba desde exactamente la carpeta `API/`. Se rompía en CI, Docker, o corriendo el `.dll` compilado desde otro lado. Ahora:

  ```csharp
  var productsFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "products.json");
  ```

  Y se configuró `Infrastructure.csproj` para copiar el JSON al output al compilar:

  ```xml
  <ItemGroup>
    <None Include="Data/SeedData/products.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  ```

- **Se eliminó `WeatherForecastController.cs`/`WeatherForecast.cs`** — leftover del template `webapi` de `dotnet new`, sin uso.

---

### 9. Nota sobre IDs no consecutivos del seed data

Se observó que los IDs del seed/generados por SQL Server saltan de a miles (`1, 4, 5, 6, ... 1002, 1003, ...`) en vez de ser estrictamente consecutivos. No es un bug del código: SQL Server cachea bloques de valores `IDENTITY` (~1000 por defecto) para rendimiento; si el servicio se reinicia o hay un rollback, los valores cacheados no usados se pierden y el próximo insert salta al siguiente bloque. Es cosmético — los IDs son solo PK, no importan sus valores mientras sean únicos.

---

### 10. DTOs en los endpoints de `Product` (entrada y salida)

Antes `ProductController` bindeaba y devolvía la entidad `Core.Entities.Product` directo. Problemas: acopla el contrato HTTP al modelo de dominio (cualquier cambio en la entidad rompe al cliente), expone campos que el cliente no debería setear (`Id` en el create) y obliga a mandar todas las propiedades `required` aunque no apliquen a la operación.

Se agregaron tres DTOs en `API/Dtos/Products/`:

| DTO | Uso | Diferencia con la entidad |
|---|---|---|
| `CreateProductDto` | body de `POST /api/product` | sin `Id` |
| `UpdateProductDto` | body de `PUT /api/product/{id}` | con `Id` (se valida contra el de la ruta) |
| `ProductDto` | payload de salida en todos los GET / POST / PUT | espejo de la entidad, pero desacoplado del dominio |

El mapeo entidad → DTO es un método de extensión, `API/Extensions/ProductMappings.cs`:

```csharp
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        PictureUrl = product.PictureUrl,
        Type = product.Type,
        Brand = product.Brand,
        QuantityInStock = product.QuantityInStock
    };
}
```

El controller ya no expone la entidad en ninguna dirección:

```csharp
[HttpGet]
public async Task<ActionResult> GetProducts(string? brand, string? type, string? sort)
{
    var products = await repository.GetProductsAsync(brand, type, sort);
    return ApiOk(products.Select(p => p.ToDto()));   // Select perezoso: se materializa al serializar
}

[HttpPost]
public async Task<ActionResult> CreateProduct(CreateProductDto dto)
{
    var product = new Product { Name = dto.Name, /* ...resto de campos... */ };
    repository.AddProduct(product);

    if (await repository.SaveChangesAsync())
        return ApiCreated(product.ToDto(), nameof(GetProductById), new { id = product.Id }, "Product created successfully");

    return ApiError(StatusCodes.Status400BadRequest, "Problem creating the product", ErrorCodes.Product.CreateError);
}
```

El mapeo se dejó manual (sin AutoMapper / Mapster): una entidad, pocos campos; una librería de mapeo agrega dependencia y magia en runtime sin ganancia real a esta escala.

> `GetBrands` / `GetTypes` siguen devolviendo `IReadOnlyList<string>` directo — son valores primitivos, no necesitan DTO.

Combinado con el contrato de respuesta (sección 3), un `GET /api/product/1` ahora responde:

```json
{
  "statusCode": 200,
  "message": "Request successful",
  "data": {
    "id": 1,
    "name": "Angular Speedster Board 2000",
    "price": 100.00,
    "pictureUrl": "...",
    "type": "Boards",
    "brand": "Angular",
    "quantityInStock": 100
  }
}
```

Con esto queda cerrado el ítem de `REVIEW.md` sobre "falta de DTOs en los endpoints de `Product`" (antes solo estaban los de create/update).

---

### 11. Falsos positivos de `IDE0005` en VS Code — `GenerateDocumentationFile`

El analizador `IDE0005` ("using directive is unnecessary") de Roslyn no se ejecuta a nivel de compilación completa si el proyecto no genera el archivo de documentación XML. Sin él, el language server de C# en VS Code marca **todos** los `using` como innecesarios, pero al quitarlos el build real falla (`CS0246`).

Fix en `API/API.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

`CS1591` (falta comentario XML en miembro público) se silencia: no se documenta la API con XML docs, solo se quería habilitar el analizador de usings. Tras el cambio hay que recargar la ventana de VS Code para que el servidor de C# tome la nueva configuración.

---

### 12. Validación de entrada con DataAnnotations en los DTOs

Los DTOs de create/update no tenían ninguna regla: `Price` podía ser `0` o negativo, `PictureUrl` cualquier string, los textos sin límite de largo. Se agregaron atributos de `System.ComponentModel.DataAnnotations`.

#### Dónde va la validación — DTO, no entidad

Las reglas van en `CreateProductDto`/`UpdateProductDto` (capa `API`), **no** en `Core.Entities.Product`. `Core` no debe depender de reglas de presentación: qué es un input válido para un endpoint HTTP es un asunto de la API, no del dominio. Además `DataAnnotations` de validación solo las mira MVC al bindear un body; en la entidad no harían nada útil.

#### Las tres capas que ya estaban enchufadas

1. **`[ApiController]`** (en `BaseApiController`) valida el modelo *antes* de entrar al método. Si falla, corta y devuelve `400` sin ejecutar el código del controller.
2. La **`InvalidModelStateResponseFactory`** (sección 6) traduce ese `400` al contrato `ApiErrorResponse`.
3. Los mensajes de cada atributo terminan en `Details` (solo en `Development`).

Por eso alcanzó con agregar atributos: no se tocó ni el controller ni `Program.cs`.

#### `required` (palabra clave) vs `[Required]` (atributo)

Son capas distintas y se usan las dos:

| | `required` (C#) | `[Required]` |
|---|---|---|
| Actúa en | deserialización (System.Text.Json) | validación de modelo (MVC) |
| Detecta | propiedad **ausente** del body | `null` **y** string vacío `""` |

`{ "name": "" }` pasa el `required` (la propiedad está) pero lo frena `[Required]`.

#### Atributos aplicados (`CreateProductDto`)

```csharp
[Required, MaxLength(100)]          public required string Name { get; set; }
[Required, MaxLength(1000)]         public required string Description { get; set; }
[Range(0.01, 999999.99)]           public decimal Price { get; set; }
[Required, Url, MaxLength(2048)]    public required string PictureUrl { get; set; }
[Required, MaxLength(100)]          public required string Type { get; set; }
[Required, MaxLength(100)]          public required string Brand { get; set; }
[Range(0, int.MaxValue)]           public int QuantityInStock { get; set; }
```

- `Price`/`QuantityInStock` no llevan `[Required]`: son value types no-nulables, nunca llegan "vacíos"; el `[Range]` cubre el "positivo".
- Los literales de `[Range]` sobre `decimal` son `double` y el atributo los convierte. La variante estricta sería `[Range(typeof(decimal), "0.01", "999999.99")]` (parsea el string sin pasar por `double`; en .NET 10 usa cultura invariante por defecto).
- En `ErrorMessage`, `{0}` = nombre del campo, `{1}`/`{2}` = límites del `Range`.

#### `UpdateProductDto` hereda, no duplica

`UpdateProductDto` tenía exactamente los mismos campos que `CreateProductDto` más `Id`. En vez de copiar campos y atributos:

```csharp
public class UpdateProductDto : CreateProductDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
```

Una sola fuente de verdad para las reglas. Contrapartida: acopla ambos DTOs; si algún día divergen (p. ej. update no permite cambiar `Brand`) hay que romper la herencia, probablemente hacia una base `abstract ProductInputDto`. Hoy son idénticos salvo `Id`, así que herencia directa alcanza.

`ProductDto` (salida) queda sin atributos: el cliente lo recibe, nunca lo envía.

#### Respuesta ante un body inválido

`POST /api/product` con `{ "price": 0, "pictureUrl": "abc", ... }`:

```json
{
  "title": "Validation failed",
  "status": 400,
  "errorCode": "VALIDATION_ERROR",
  "details": "El precio debe estar entre 0.01 y 999999.99. | The PictureUrl field is not a valid fully-qualified http, https, or ftp URL."
}
```

`details` solo en `Development`; en otro entorno, únicamente `title` + `errorCode`.

---

### 13. Paginación (`GET /api/product`)

`GetProducts` devolvía **todos** los productos que matcheaban `brand`/`type`/`sort` en una sola respuesta. No urgía con 20 productos de seed, pero crece mal con un catálogo real. Se agregó paginación con `pageIndex`/`pageSize`.

#### Dónde va el corte — repositorio, no controller

`Skip`/`Take` se traducen a SQL (`OFFSET`/`FETCH`) solo si se aplican **antes** de materializar el `IQueryable` con `ToListAsync()`. Si se paginara en el controller sobre una `List<Product>` ya traída completa, EF Core habría descargado la tabla entera igual — la paginación no serviría de nada. Por eso `pageIndex`/`pageSize` bajan hasta `ProductRepository`, no se resuelven en el controller.

#### El conteo va antes de paginar, después de filtrar

```csharp
var query = context.Products.AsQueryable();
// ...Where de brand/type...

var count = await query.CountAsync();   // total de resultados para estos filtros

query = sort switch { /* OrderBy */ };  // el orden no cambia cuántos hay

var items = await query
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

`count` tiene que ser el total de productos que matchean el filtro, no el total de la tabla ni el tamaño de la página actual — si no, el cliente no puede calcular cuántas páginas hay. Por eso se cuenta sobre `query` ya filtrada pero antes de `Skip`/`Take`. Contarlo antes o después del `OrderBy` da igual (ordenar no agrega ni quita filas), pero se dejó antes por claridad.

`IProductRepository.GetProductsAsync` ahora devuelve una tupla `(IReadOnlyList<Product> Items, int Count)` en vez de solo la lista — sin crear una clase nueva en `Core` solo para cargar dos valores.

#### Saneamiento de `pageIndex`/`pageSize` en el controller

```csharp
private const int MaxPageSize = 50;

pageIndex = Math.Max(pageIndex, 1);
pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
```

No se confía en lo que llega en la query string:

- **`Math.Max(pageIndex, 1)`** — si mandan `pageIndex=0` o negativo, `Skip((0-1)*6)` sería `Skip(-6)`, y `Skip` no acepta negativos (EF Core lanza excepción). Se fuerza a `1`.
- **`Math.Clamp(pageSize, 1, MaxPageSize)`** — acota entre `1` y `50`. Por abajo evita `Take(0)`/`Take(-5)`; por arriba evita que alguien pida `pageSize=9999` y traiga el catálogo completo de un solo request (carga innecesaria a la DB y a la red).

Es la misma idea de *fail fast*/*guard clause*: sanear la entrada en el borde antes de que un valor inválido se propague al `Skip`/`Take`.

#### Envoltorio de respuesta — `Pagination<T>`

```csharp
public class Pagination<T>(int pageIndex, int pageSize, int count, IReadOnlyList<T> items)
{
    public int PageIndex { get; set; } = pageIndex;
    public int PageSize { get; set; } = pageSize;
    public int Count { get; set; } = count;
    public IReadOnlyList<T> Items { get; set; } = items;
}
```

Vive en `API/Dtos/Pagination.cs` (no en `API/Dtos/Products/`) porque no es específico de `Product` — cualquier lista paginada de la API puede reutilizarlo.

El campo se llama `Items`, no `Data`. `ApiResponse<T>` ya envuelve toda respuesta 2xx en un campo `data` (ver sección 3); si `Pagination<T>` también usara `Data` para su lista, el JSON final tendría `data.data` — el mismo nombre significando dos cosas distintas en dos niveles (*property stuttering*). Con `Items` queda `data.items`, sin ambigüedad:

```json
{
  "statusCode": 200,
  "message": "Request successful",
  "data": {
    "pageIndex": 1,
    "pageSize": 6,
    "count": 20,
    "items": [ /* productos de esta página */ ]
  }
}
```

#### Uso

```
GET /api/product?pageIndex=2&pageSize=10&brand=Nike&sort=priceDesc
```

Defaults si se omiten: `pageIndex=1`, `pageSize=6`.

---

### Pendientes

Ver `REVIEW.md` en la raíz del repo — lista de mejoras abiertas clasificadas por urgencia (CORS sin restringir por entorno, falta de DTOs en los endpoints de `Product`, etc).
