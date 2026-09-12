# SPEC 04 — Assets reales de producto en el backend

> **Status:** Implementada
> **Depends on:** SPEC 03
> **Date:** 2026-09-12
> **Objective:** Servir las 18 imágenes reales de producto desde `API/wwwroot/images/products/` vía `UseStaticFiles`, y hacer que `ProductCard` arme la URL absoluta correcta para que `PictureUrl` resuelva de verdad en la home.

---

## Por qué existe este spec

SPEC 03 dejó explícitamente fuera de alcance las imágenes reales: no existía `wwwroot` ni `UseStaticFiles` en el API, y no había archivos de imagen en el repo — el frontend cubría el hueco con un placeholder (`@error` en `ProductCard`). Ahora las 18 imágenes ya existen en disco (`/Users/jonathannieto/Downloads/CourseAssets/images/products`) con los mismos nombres que usa `PictureUrl` en el seed (`sb-ang1.png`, `boot-ang1.png`, etc.), así que corresponde resolver el gap real: servirlas desde el backend y corregir cómo el frontend arma la URL de imagen (hoy `PictureUrl` es una ruta relativa que el `<img>` resuelve contra el origen del frontend, `localhost:3000`, no contra el API, `localhost:7075`).

---

## Scope

**In:**

- Copiar los 18 PNG de `/Users/jonathannieto/Downloads/CourseAssets/images/products` a `API/wwwroot/images/products/`, con los mismos nombres de archivo.
- Agregar `app.UseStaticFiles();` en `API/Program.cs`.
- Frontend: `ProductCard.vue` arma la URL absoluta de la imagen componiendo el origen del API (derivado de `runtimeConfig.public.apiBase`, quitando el sufijo `/api`) + `product.pictureUrl` (ej. `https://localhost:7075` + `/images/products/sb-ang1.png`), en vez de usar `product.pictureUrl` tal cual.
- Mantener el fallback `@error` existente en `ProductCard.vue` (placeholder de ícono) como red de seguridad para productos futuros sin imagen real.
- Esto solo necesita funcionar en Development por ahora — `wwwroot`/`UseStaticFiles` funciona igual en cualquier entorno de ASP.NET Core sin configuración adicional, así que no hace falta tocar `appsettings.Production.json` (pendiente aparte, ver `REVIEW.md`) para que esto funcione el día que exista.

**Out of scope (para specs futuros):**

- Página de detalle de producto.
- CDN, storage externo (S3/Azure Blob) u otra estrategia de hosting de imágenes — se sirven desde el propio `wwwroot` del API.
- Subida de imágenes vía `CreateProductDto`/`UpdateProductDto` (los DTOs ya validan `PictureUrl` como URL de texto; no cambia en este spec).
- Optimización/redimensionado de imágenes (`@nuxt/image` u otro).
- `appsettings.Production.json` / CORS de producción.

---

## Data model

No se introducen estructuras de datos nuevas. `ProductDto.PictureUrl` sigue siendo el mismo string relativo (`/images/products/sb-ang1.png`); lo que cambia es solo cómo el frontend lo consume para armar el `src` del `<img>`.

---

## Implementation plan

1. **Backend — assets:** copiar los 18 archivos de `/Users/jonathannieto/Downloads/CourseAssets/images/products` a `API/wwwroot/images/products/` (crear la carpeta `wwwroot` si no existe).
2. **Backend — static files:** agregar `app.UseStaticFiles();` en `API/Program.cs`.
3. **Frontend — URL de imagen:** en `ProductCard.vue`, calcular el origen del API a partir de `useRuntimeConfig().public.apiBase` (quitando el sufijo `/api`) y componer `imageUrl = apiOrigin + product.pictureUrl` como `computed`; usar `imageUrl` en el `src` del `<img>` en vez de `product.pictureUrl` directo.
4. **Verificación manual** (a cargo del usuario): `dotnet run --project API`, `pnpm dev`/`bun dev` en `frontend`; confirmar que las 10 cards de la home muestran las imágenes reales (sin 404 en consola), y que un `PictureUrl` roto a propósito (ej. editando un producto vía `PUT /api/product/{id}` con una URL inexistente) sigue cayendo en el placeholder de `@error`.

---

## Acceptance criteria

- [x] `API/wwwroot/images/products/` contiene los 18 PNG con los mismos nombres que `Infrastructure/Data/SeedData/products.json`.
- [x] `API/Program.cs` incluye `app.UseStaticFiles();`.
- [x] `GET https://localhost:7075/images/products/sb-ang1.png` (y el resto) responde 200 con la imagen, no 404.
- [x] La home (`http://localhost:3000`) muestra las 10 cards con sus imágenes reales cargando correctamente, sin 404 de `PictureUrl` en consola.
- [x] `ProductCard.vue` arma la URL de imagen a partir de `apiBase`, no usa `product.pictureUrl` como `src` directo.
- [x] El fallback de placeholder (`@error`) sigue funcionando si a un producto le falta la imagen real (verificable forzando un `PictureUrl` inexistente).
- [x] La app levanta sin errores de consola ni de CORS.

---

## Decisions

- **Sí:** servir las imágenes desde `wwwroot` del propio API en vez de un storage externo (S3/Azure Blob/CDN). Es la solución más simple para el estado actual del proyecto (sin infraestructura cloud todavía); se reevalúa si el catálogo crece mucho o se necesita CDN por performance.
- **Sí:** componer la URL absoluta en el frontend (`apiOrigin + pictureUrl`) en vez de que el backend devuelva URLs absolutas en `ProductDto`. Mantiene al backend agnóstico de su propio host público (relevante también quándo exista `appsettings.Production.json` con un dominio distinto) y el cambio queda contenido en `ProductCard.vue`.
- **Sí:** mantener el fallback `@error` de SPEC 03 en vez de quitarlo. Sigue siendo la red de seguridad correcta para productos que se creen a futuro sin una imagen real subida.
- **No:** implementar subida de imágenes o cambios a los DTOs de producto. Las 18 imágenes se copian directo al repo; subir nuevas imágenes vía la API es un problema distinto (multipart upload, validación de tipo de archivo, storage) que no se discutió.
- **No:** resolver `appsettings.Production.json` en este spec. `UseStaticFiles` no necesita configuración por entorno; ese pendiente sigue siendo el de `REVIEW.md`.

---

## What is **not** in this spec

- Página de detalle de producto.
- Storage externo/CDN para imágenes.
- Subida de imágenes vía la API.
- Optimización de imágenes en el frontend.
- CORS/config de producción.

Cada uno de estos, si se implementa, va en su propio spec.
