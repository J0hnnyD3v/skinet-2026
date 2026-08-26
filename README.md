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
