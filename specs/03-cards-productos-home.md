# SPEC 03 — Cards de productos con imagen en la Home

> **Status:** Implementada
> **Depends on:** SPEC 01, SPEC 02
> **Date:** 2026-09-12
> **Objective:** Reemplazar el placeholder de `app/pages/index.vue` por un grid de cards de producto (imagen, nombre, marca, tipo y precio) que trae los datos reales desde `GET /api/product` del backend.

---

## Por qué existe este spec

SPEC 01 dejó `index.vue` vacío a propósito ("sin Hero/features/CTA genéricos, sin catálogo"). El backend ya tiene `ProductController.GetProducts` con paginación y `ProductDto`, pero el frontend nunca hizo una llamada real al API — esta es la primera integración Nuxt → API de todo el proyecto. Eso obliga a resolver, además de la UI de las cards, tres cosas que no existían antes: CORS del puerto de Nuxt, la estrategia de fetch de datos dinámicos en una ruta que hoy está marcada `prerender: true`, y qué hacer con las imágenes del seed que no resuelven a nada real.

---

## Scope

**In:**

- Backend: agregar `http://localhost:3000` y `https://localhost:3000` a `Cors:AllowedOrigins` en `API/appsettings.Development.json`.
- Backend: cambiar el default de `pageSize` en `ProductController.GetProducts` de `6` a `10`.
- Frontend: quitar `routeRules: { '/': { prerender: true } }` de `nuxt.config.ts` — la home pasa a pedir datos frescos al API en cada visita (SSR/CSR normal, sin prerender estático).
- Frontend: agregar `runtimeConfig.public.apiBase` en `nuxt.config.ts` apuntando a `https://localhost:7075/api` (Development), configurable por variable de entorno (`NUXT_PUBLIC_API_BASE`).
- Frontend: tipo `Product` (name, description, price, pictureUrl, type, brand, quantityInStock, id) en `app/types/product.ts`, reflejando `ProductDto`.
- Frontend: composable `app/composables/useProducts.ts` que llama a `GET {apiBase}/product` (sin filtros, `pageSize=10`) vía `useFetch`, devolviendo `products`, `pending` y `error`.
- Frontend: componente `app/components/ProductCard.vue` — recibe un `Product` como prop y muestra: imagen, nombre, marca y tipo (como texto secundario o badges), precio formateado en pesos mexicanos (`Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })`). Si la imagen no carga (`@error`), muestra un placeholder genérico en su lugar (ícono o imagen gris de "sin imagen"), sin llamar a ningún endpoint adicional.
- Frontend: `app/pages/index.vue` — grid responsive (`UPageGrid` o CSS grid con Tailwind) que renderiza un `ProductCard` por cada producto de `useProducts()`. Mientras `pending` es `true`, muestra `USkeleton` (uno por cada card esperada, ej. 10). Si `error` no es null, muestra un mensaje de texto simple ("No se pudieron cargar los productos.") en vez del grid.
- Las cards son puramente visuales: el clic en una card no navega a ningún lado (no existe página de detalle de producto todavía).

**Out of scope (para specs futuros):**

- Página de detalle de producto (`/shop/[id]`) y navegación desde la card.
- Imágenes reales de producto en el backend (`wwwroot`, `UseStaticFiles`, copiar los PNGs del seed original de Angular/Skinet a un directorio servido). El `PictureUrl` del seed sigue sin resolver; el placeholder del frontend cubre ese caso.
- Filtros, búsqueda, orden o paginación visible en la home (`brand`, `type`, `sort`, `search`, `pageIndex` del endpoint no se usan todavía).
- Agregar al carrito desde la card (no hay lógica de carrito conectada en el frontend todavía).
- `appsettings.Production.json` / CORS de producción (sigue pendiente en `REVIEW.md`).
- Cache de `GetBrands`/`GetTypes` u otras mejoras de `ProductController` no relacionadas con esta spec.

---

## Data model

No se introducen entidades nuevas en el backend (se reutiliza `ProductDto` existente). En el frontend se introduce:

```ts
// app/types/product.ts
export interface Product {
  id: number
  name: string
  description: string
  price: number
  pictureUrl: string
  type: string
  brand: string
  quantityInStock: number
}
```

Sin persistencia ni versionado — los datos viven solo en memoria del componente mientras dura la visita (sin store global; se reevalúa este punto cuando exista carrito o filtros).

---

## Implementation plan

1. **Backend — CORS:** agregar `http://localhost:3000` y `https://localhost:3000` al array `Cors:AllowedOrigins` de `API/appsettings.Development.json`.
2. **Backend — pageSize:** cambiar `int pageSize = 6` por `int pageSize = 10` en la firma de `ProductController.GetProducts`.
3. **Frontend — config:** en `nuxt.config.ts`, quitar el bloque `routeRules` de SPEC 01 y agregar `runtimeConfig: { public: { apiBase: process.env.NUXT_PUBLIC_API_BASE || 'https://localhost:7075/api' } }`.
4. **Frontend — tipos:** crear `app/types/product.ts` con la interfaz `Product`.
5. **Frontend — fetch:** crear `app/composables/useProducts.ts` usando `useFetch<Pagination<Product>>` (o el shape equivalente que devuelve `ApiOk`) contra `${apiBase}/product?pageSize=10`, extrayendo `.data.data` como lista de productos.
6. **Frontend — card:** crear `app/components/ProductCard.vue` con imagen (+ fallback `@error` a placeholder), nombre, badges de marca/tipo y precio formateado con `Intl.NumberFormat('es-MX', ...)`.
7. **Frontend — home:** reescribir `app/pages/index.vue` para usar `useProducts()`, renderizar el grid de `ProductCard`, `USkeleton` mientras `pending`, y el mensaje de error si `error` está presente.
8. **Verificación manual** (a cargo del usuario): levantar `docker compose up -d`, `dotnet run --project API`, y `pnpm dev`/`bun dev` en `frontend`; confirmar que la home en `http://localhost:3000` muestra 10 cards con nombre/marca/tipo/precio, placeholder en las imágenes (ya que no resuelven), skeleton momentáneo al cargar, y que apagar el backend muestra el mensaje de error — todo sin errores de CORS ni de consola.

---

## Acceptance criteria

- [x] `API/appsettings.Development.json` incluye `http://localhost:3000` y `https://localhost:3000` en `Cors:AllowedOrigins`.
- [x] `ProductController.GetProducts` usa `pageSize = 10` como default.
- [x] `nuxt.config.ts` ya no tiene `routeRules` para `/`, y expone `runtimeConfig.public.apiBase`.
- [x] `app/pages/index.vue` muestra un grid con 10 `ProductCard` obtenidos de `GET /api/product` real (verificable viendo la petición en Network del navegador).
- [x] Cada `ProductCard` muestra imagen (o placeholder si `PictureUrl` no resuelve), nombre, marca, tipo y precio formateado como moneda MXN.
- [x] Mientras la petición está en curso se ven `USkeleton`; no se ve el grid final ni un flash vacío antes de eso.
- [x] Si el backend no responde, la home muestra el mensaje de error en vez de romper o quedar en blanco.
- [x] El clic en una card no navega a ningún lado ni lanza errores en consola.
- [x] La app levanta sin errores de CORS ni de consola del navegador o del servidor de desarrollo, salvo los 404 esperados de `PictureUrl` sin resolver (cubiertos por el placeholder de imagen; se resuelven en SPEC 04).

---

## Decisions

- **Sí:** agregar `localhost:3000` al CORS de dev en este spec, en vez de dejarlo como bloqueante externo. Es un cambio de una línea y sin él ninguna integración frontend-backend funciona.
- **Sí:** quitar `prerender: true` de `/`. Con contenido dinámico traído del API en cada visita, el prerender de build congelaría los datos (o rompería el build si el API no está arriba). SSR/CSR normal es lo correcto para un catálogo.
- **Sí:** usar un placeholder de imagen en el frontend (`@error` en el `<img>`) en vez de arreglar el backend para servir las imágenes reales del seed. Arreglar `wwwroot`/`UseStaticFiles` es trabajo de backend no relacionado con la UI de las cards; queda para una spec futura de "assets reales de producto".
- **Sí:** subir el `pageSize` default de 6 a 10, a pedido explícito del usuario, en vez de agregar paginación visible en la home todavía.
- **No:** navegar a una página de detalle al hacer clic en la card. No existe esa página aún; se prefiere no inventar una ruta placeholder extra fuera del pedido original.
- **No:** usar un store global (Pinia) para los productos de la home. Con una sola pantalla consumiendo los datos, un composable con `useFetch` alcanza; se reevalúa si el carrito o los filtros necesitan compartir este estado.
- **Sí:** formatear el precio en pesos mexicanos (`es-MX`/`MXN`) siguiendo la convención de que el usuario es mexicano, en vez de dejar el precio crudo o en USD.

---

## What is **not** in this spec

- Página de detalle de producto y navegación desde las cards.
- Imágenes reales de producto servidas por el backend.
- Filtros, búsqueda, orden y paginación visible en la home.
- Agregar productos al carrito desde la card.
- CORS/config de producción.

Cada uno de estos, si se implementa, va en su propio spec.
