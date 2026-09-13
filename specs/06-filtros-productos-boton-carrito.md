# SPEC 06 — Filtros de productos (modal) y botón "Add to cart" (solo UI)

> **Status:** Implementada
> **Depends on:** SPEC 03, SPEC 05
> **Date:** 2026-09-13
> **Objective:** Agregar un modal de filtros por marca/tipo (multi-selección) sobre el grid de la home, ampliando el backend para soportar listas de valores, y un botón "Add to cart" puramente visual en cada `ProductCard`.

---

## Por qué existe este spec

La home ya trae productos reales (SPEC 03/04) con estilos definitivos (SPEC 05), pero no hay forma de acotar el catálogo — `ProductController.GetBrands`/`GetTypes` existen pero nada los consume, y `GetProducts` solo filtra por un `brand`/`type` exacto (`x.Brand == brand`), no por varios a la vez. El usuario compartió un mockup de un modal de filtros con checkboxes múltiples por marca y tipo, más un botón "Add to cart" en cada card. El carrito real (`CartService`/`CartController` con Redis) ya existe en el backend pero conectarlo implica resolver identidad del carrito (el `key` no está atado a ningún cliente — pendiente marcado en `REVIEW.md`) y sincronizar el badge del header; eso queda fuera de este spec a pedido explícito: el botón se agrega ahora solo como UI, sin funcionalidad, y la integración real del carrito va en un spec futuro.

---

## Scope

**In:**

- **Backend — filtro multi-valor:** `ProductController.GetProducts` acepta `brand`/`type` como `string[]?` en vez de `string?` (bindeados desde query string repetida, ej. `?brand=Angular&brand=React`). `IProductRepository.GetProductsAsync` y `ProductRepository.GetProductsAsync` actualizan su firma para recibir `IReadOnlyList<string>?` y filtran con `.Contains()` sobre esas listas (sin filtrar si la lista viene vacía o nula, igual que hoy con `string.IsNullOrWhiteSpace`).
- **Frontend — modal de filtros:** nuevo componente `app/components/ProductFiltersModal.vue`, construido con `UModal` de Nuxt UI, con dos columnas de checkboxes ("Brands" y "Types") pobladas dinámicamente desde `GET /api/product/brands` y `GET /api/product/types`. Incluye un botón "Apply Filters" (dispara el fetch con los filtros marcados) y un botón "Clear" (desmarca todo y vuelve a traer productos sin filtro).
- **Frontend — trigger del modal:** un `UButton` "Filters" (ícono de embudo, ej. `i-lucide-filter`) arriba a la derecha del grid de productos en `app/pages/index.vue`, que abre `ProductFiltersModal`.
- **Frontend — integración con el fetch de productos:** `useProducts.ts` acepta los filtros seleccionados (brands/types) y los manda como query params repetidos al llamar `GET /api/product`; al aplicar o limpiar filtros, el grid se refresca con los nuevos resultados (reutilizando el `USkeleton`/estado de error ya existentes de SPEC 03).
- **Frontend — botón "Add to cart":** en `ProductCard.vue`, un `UButton` de ancho completo debajo del precio, con ícono de carrito (`i-lucide-shopping-cart`) y texto "Add to cart". Sin `@click` funcional (o un placeholder vacío) — no llama a ningún endpoint ni modifica estado.

**Out of scope (para specs futuros):**

- Conectar el botón "Add to cart" al `CartService`/`CartController` real (Redis) — incluye resolver cómo se genera/persiste el identificador del carrito en el frontend (cookie `buyerId` anónima u otro mecanismo, pendiente también en `REVIEW.md`), armar/actualizar el `ShoppingCart`, y sincronizar el badge del carrito en el header (hoy fijo en `0`).
- Filtro por `search` (texto) o `sort` (orden) — el endpoint ya los soporta pero no se exponen en el modal de esta spec.
- Paginación visible en la home (`pageIndex`) — sigue trayendo una sola página de 10 resultados, ahora filtrados.
- Persistir los filtros seleccionados en la URL (query params de la ruta) o entre sesiones — el estado de filtros vive solo en memoria del componente mientras dura la visita.
- Página de detalle de producto.
- Contenido real de `/shop` (que podría reusar este mismo modal a futuro).

---

## Data model

No se agregan entidades de dominio nuevas. Cambios de forma en contratos existentes:

**Backend** — `ProductController.GetProducts` y `IProductRepository.GetProductsAsync`/`ProductRepository.GetProductsAsync`:

```csharp
// Antes: string? brand, string? type
// Después:
Task<ActionResult> GetProducts(string[]? brand, string[]? type, string? sort, string? search, int pageIndex = 1, int pageSize = 10)

Task<(IReadOnlyList<Product> Items, int Count)> GetProductsAsync(
    IReadOnlyList<string>? brand, IReadOnlyList<string>? type, string? sort, string? search, int pageIndex, int pageSize)
```

**Frontend** — estado local (no persistido) en `app/pages/index.vue` o dentro de `ProductFiltersModal.vue`:

```ts
interface ProductFilters {
  brands: string[]
  types: string[]
}
```

---

## Implementation plan

1. **Backend — repositorio:** en `Infrastructure/Data/ProductRepository.cs`, cambiar `GetProductsAsync` para recibir `IReadOnlyList<string>? brand, IReadOnlyList<string>? type` y filtrar con `brand is { Count: > 0 }` + `.Where(x => brand.Contains(x.Brand))` (mismo patrón para `type`).
2. **Backend — interfaz:** actualizar la firma en `Core/Interfaces/IProductRepository.cs` para que coincida.
3. **Backend — controller:** en `API/Controllers/ProductController.cs`, cambiar `GetProducts` para recibir `string[]? brand, string[]? type` y pasarlos tal cual al repositorio.
4. **Frontend — opciones de filtro:** crear un composable o fetch inline (`useFetch`) para `GET {apiBase}/product/brands` y `GET {apiBase}/product/types`, usado por `ProductFiltersModal.vue` para poblar los checkboxes.
5. **Frontend — modal de filtros:** crear `app/components/ProductFiltersModal.vue` (`UModal`) con las dos columnas de checkboxes, botón "Apply Filters" (emite los brands/types seleccionados al padre) y botón "Clear" (limpia selección y emite filtros vacíos).
6. **Frontend — home:** en `app/pages/index.vue`, agregar el `UButton` "Filters" arriba a la derecha del grid, mantener el estado `ProductFilters` seleccionado, y pasarlo a `useProducts()`.
7. **Frontend — fetch con filtros:** actualizar `useProducts.ts` para aceptar `brands`/`types` (reactivos) y mandarlos como `query: { brand: filters.brands, type: filters.types, pageSize: 10 }` a `useFetch` (que serializa arrays como query params repetidos), re-disparando el fetch cuando cambian.
8. **Frontend — botón add to cart:** en `ProductCard.vue`, agregar el `UButton` de ancho completo con ícono de carrito y texto "Add to cart" debajo del precio, sin lógica.
9. **Verificación manual** (a cargo del usuario): abrir el modal, marcar varias marcas y tipos, aplicar, confirmar en Network que la request usa `?brand=X&brand=Y` y que el grid muestra solo los productos que matchean; limpiar filtros y confirmar que vuelve a mostrar los 10 originales; confirmar que cada card tiene el botón "Add to cart" visualmente correcto y que hacer clic no produce errores ni llamadas de red.

---

## Acceptance criteria

- [x] `ProductController.GetProducts` acepta `brand`/`type` como arrays desde la query string.
- [x] `IProductRepository`/`ProductRepository.GetProductsAsync` filtran por lista de brands/types (no un solo valor).
- [x] El endpoint sigue funcionando igual que antes cuando no se manda `brand`/`type` (sin filtrar, mismo comportamiento que hoy).
- [x] En la home aparece un botón "Filters" arriba a la derecha del grid que abre un modal.
- [x] El modal muestra checkboxes de Brands y Types poblados desde `GET /api/product/brands` y `GET /api/product/types` reales.
- [x] Marcar varias marcas/tipos y hacer clic en "Apply Filters" refresca el grid mostrando solo productos que matchean esos filtros (verificable en Network: request con `brand`/`type` repetidos).
- [x] El botón "Clear" del modal desmarca todo y vuelve a traer los productos sin filtrar.
- [x] Cada `ProductCard` muestra un botón "Add to cart" de ancho completo con ícono, debajo del precio.
- [x] Hacer clic en "Add to cart" no dispara ninguna llamada de red ni error en consola (es puramente visual).
- [x] La app levanta sin errores de consola en ningún estado (con o sin filtros aplicados).

---

## Decisions

- **Sí:** ampliar el backend para aceptar `brand`/`type` como listas en vez de limitar el modal a selección única. El usuario decidió explícitamente esto tras ver que el mockup requiere multi-selección real.
- **Sí:** dejar el botón "Add to cart" sin funcionalidad en este spec, a pedido explícito del usuario. Conectar el carrito real implica resolver identidad del carrito (buyerId) y sincronizar el badge del header — se trata en un spec futuro dedicado.
- **Sí:** meter el modal de filtros y el botón de carrito (aunque sea solo UI) en un mismo spec, a pesar de tocar varios dominios (backend + modal + integración de fetch + UI de card). El usuario decidió explícitamente no dividirlo tras la advertencia inicial.
- **Sí:** usar `UModal` de Nuxt UI para el modal de filtros, consistente con el resto del proyecto (`UDropdownMenu`, `USlideover` ya usados en el header).
- **No:** exponer `search`/`sort` en este modal — el endpoint ya los soporta pero no se discutieron specs de UI para ellos.
- **No:** persistir los filtros seleccionados en la URL o entre sesiones. Viven en memoria del componente; se reevalúa si se necesita compartir/bookmarkear una vista filtrada.
- **No:** agregar paginación visible al filtrar. Sigue trayendo una sola página de hasta 10 resultados, como en SPEC 03.
- **Desvíos surgidos durante la implementación (no anticipados en el plan):**
  - `[FromQuery]` explícito en `brand`/`type` de `ProductController.GetProducts`: sin esa anotación, `[ApiController]` infería (mal) que dos parámetros de tipo array debían bindearse desde el body, y la app no arrancaba (`InvalidOperationException`).
  - El modal usa `UCheckboxGroup` (con `items`/`v-model` de array) en vez de `UCheckbox` individuales — es el componente que Nuxt UI expone específicamente para grupos de checkboxes con selección múltiple ligada a un array.
  - Aparecieron dos warnings de hidratación de Vue en la home: como `useProducts` usa `server: false` (fix de SPEC 05 para el certificado autofirmado), el servidor y el cliente renderizaban árboles distintos en el primer paint (servidor sin `pending`, cliente con `pending: true`). Se resolvió envolviendo el bloque de datos en `<ClientOnly>` con un `#fallback` de skeletons, para que servidor y cliente coincidan.
  - El diseño del modal y de los botones "Filters"/"Add to cart" se refinó varias iteraciones más allá de la descripción inicial del plan (separación de columnas con borde, footer `justify-between`, variantes de botón con más jerarquía visual, sombras/hover) a pedido explícito del usuario tras ver el resultado inicial "demasiado básico".

---

## What is **not** in this spec

- Carrito funcional (conectar "Add to cart" a `CartService`/Redis, identidad del carrito, badge dinámico del header).
- Filtro por texto (`search`) u orden (`sort`) en la UI.
- Paginación visible en la home.
- Persistencia de filtros en la URL o entre sesiones.
- Página de detalle de producto.

Cada uno de estos, si se implementa, va en su propio spec.
