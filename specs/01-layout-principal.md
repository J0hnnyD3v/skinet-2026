# SPEC 01 — Layout principal del frontend

> **Status:** Aprobada
> **Depends on:** —
> **Date:** 2026-09-12
> **Objective:** Reemplazar el scaffold genérico de Nuxt UI por un layout principal (`app/layouts/default.vue`) con header, footer y navegación mínima propios de Skinet.

---

## Por qué existe este spec

El proyecto `frontend` se inicializó con `nuxi init` sobre el template "Starter" de Nuxt UI. Todo el markup del layout vive hoy inline en `app.vue`, con textos, links y menús que apuntan a las demos del template (`starter-template.nuxt.dev`, repo de GitHub del template, etc.). Antes de construir cualquier página real (catálogo, carrito) hace falta un layout base propio, aislado en `app/layouts/`, sin boilerplate ajeno a Skinet.

---

## Scope

**In:**

- Crear `app/layouts/default.vue` usando `UHeader` / `UMain` / `UFooter` de Nuxt UI, envuelto por `<NuxtLayout>` en `app.vue`.
- Header con: logo (placeholder actual de Nuxt UI, sin cambios), un link de navegación "Home", un slot/espacio reservado vacío para el futuro ícono de carrito, y el `UColorModeButton` (toggle de tema).
- Footer con únicamente el copyright: `© Skinet {añoActual}`.
- Eliminar `app/components/TemplateMenu.vue` (dropdown de demos del template) y su uso en el header.
- Eliminar el botón de GitHub del header y del footer.
- Actualizar el `useSeoMeta`/`useHead` en `app.vue`: `title: 'Skinet'`, `description` genérica de e-commerce, quitar `ogImage` (apuntaba a un asset del template que ya no aplica) y `twitterCard`.
- Vaciar `app/pages/index.vue`: dejarla como página mínima (`<template><div /></template>` o equivalente), sin el Hero/features/CTA genéricos.

**Out of scope (para specs futuros):**

- Contenido real de la Home (catálogo de productos).
- Ícono de carrito funcional (contador, integración con `CartService`/`CartController`/Redis del backend).
- Navegación adicional (Productos, categorías, login, etc.).
- Rediseño del logo (`AppLogo.vue` se mantiene tal cual, con el SVG placeholder de Nuxt UI).
- Layouts alternativos (ej. uno sin header para checkout).

---

## Data model

Esta spec no introduce estructuras de datos nuevas. Es puramente de UI/estructura de layout.

---

## Implementation plan

1. Crear `app/layouts/default.vue` moviendo el markup de `UHeader`/`UMain`/`UFooter` desde `app.vue`, sin `TemplateMenu` ni el botón de GitHub, y con el footer reducido al copyright.
2. En el header del nuevo layout, agregar un `NuxtLink` a `/` con texto "Home", y un `<div>` vacío (o comentario) marcando el lugar reservado para el ícono de carrito.
3. Actualizar `app.vue` para dejar solo el `useHead`/`useSeoMeta` (con los valores de Skinet) y envolver `<NuxtPage />` en `<NuxtLayout>`.
4. Eliminar `app/components/TemplateMenu.vue`.
5. Vaciar `app/pages/index.vue` a un placeholder mínimo.
6. Verificación manual: `dotnet`/`bun`/`pnpm dev` (a cargo del usuario) y revisar visualmente header, footer, toggle de tema y que no haya errores de consola por el componente eliminado.

---

## Acceptance criteria

- [ ] `app/layouts/default.vue` existe y `app.vue` lo usa vía `<NuxtLayout>`.
- [ ] El header muestra: logo, link "Home", espacio reservado vacío, y el toggle de tema — sin el dropdown de templates ni el botón de GitHub.
- [ ] El footer muestra únicamente `© Skinet {año actual}`.
- [ ] `app/components/TemplateMenu.vue` ya no existe en el repo.
- [ ] `app/pages/index.vue` no contiene el Hero/features/CTA del template original.
- [ ] El `<title>` de la página es "Skinet" y la descripción SEO ya no menciona "Nuxt Starter Template".
- [ ] No hay referencias rotas a `TemplateMenu` ni a assets del template (ej. `ogImage` del starter) en el código.
- [ ] La app levanta sin errores en consola del navegador ni del servidor de desarrollo.

---

## Decisions

- **Sí:** usar `app/layouts/default.vue` (patrón estándar de Nuxt) en vez de dejar todo inline en `app.vue`. Permite layouts alternativos a futuro (ej. checkout) sin reescribir la estructura.
- **No:** implementar el ícono de carrito ahora, aunque el backend ya tiene `CartService`/`CartController` con Redis. Mezclaría la estructura visual del layout con lógica de estado/API; se deja como espacio reservado.
- **Sí:** eliminar `TemplateMenu.vue` en vez de reutilizarlo. Su contenido (links a demos externas de Nuxt UI) no tiene ningún valor para Skinet.
- **No:** rediseñar `AppLogo.vue` en este spec. Es un placeholder aceptado temporalmente; el branding real queda pendiente de definir.
- **Sí:** quitar `ogImage` en vez de dejarlo apuntando al asset del template starter, para no distribuir metadata incorrecta.
- **No:** tocar `app.config.ts` (colores `primary: green` / `neutral: slate`). No se discutió y no es parte del layout estructural.

---

## What is **not** in this spec

- Contenido real de la Home / catálogo de productos.
- Ícono e integración funcional del carrito.
- Navegación más allá de "Home".
- Diseño de un logo propio para Skinet.
- Layouts alternativos a `default.vue`.

Cada uno de estos, si se implementa, va en su propio spec.
