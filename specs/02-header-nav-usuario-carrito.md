# SPEC 02 — Header: logo propio, navegación, carrito y menú de usuario

> **Status:** Implementada
> **Depends on:** SPEC 01
> **Date:** 2026-09-12
> **Objective:** Ampliar el header de SPEC 01 con el logo real de Skinet, navegación de tres ítems (Home, Shop, Contact), un ícono de carrito con badge (sin lógica todavía) y un ícono de usuario con dropdown de Login/Register.

---

## Por qué existe este spec

SPEC 01 dejó el header con logo placeholder, un solo link "Home" y un espacio vacío reservado para el carrito. Ya existe el logo real de Skinet (`frontend/public/logo.png`) y se necesita la estructura completa de navegación e íconos que va a tener el header definitivo, aunque el carrito y la autenticación todavía no tengan lógica real conectada al backend.

---

## Scope

**In:**

- Reemplazar el SVG placeholder de `AppLogo.vue` por el logo real en `public/logo.png`, usando `<img>`.
- Navegación de tres ítems en el header: **Home** (`/`), **Shop** (`/shop`), **Contact** (`/contact`).
- Crear `app/pages/shop.vue` y `app/pages/contact.vue` como páginas placeholder mínimas (mismo patrón que `index.vue` de SPEC 01: `<template><div /></template>`).
- Ícono de carrito en el header con badge de contador, **fijo en 0**, sin conexión a `CartService`/Redis todavía.
- Ícono de usuario con dropdown (`UDropdownMenu`) con dos opciones: "Login" y "Register".
- Crear `app/pages/login.vue` y `app/pages/register.vue` como páginas placeholder mínimas, a las que navegan esas dos opciones del dropdown.

**Out of scope (para specs futuros):**

- Lógica real del carrito (leer `CartService`/Redis, contador dinámico, agregar/quitar productos).
- Formularios y lógica real de Login/Register (no existe backend de auth todavía).
- Menú de navegación responsive/hamburguesa para mobile.
- Contenido real de `/shop` y `/contact` (catálogo, formulario de contacto).
- Rediseño de colores/paleta del `app.config.ts`.

---

## Data model

Esta spec no introduce estructuras de datos nuevas ni llamadas a API. El badge del carrito es un valor estático (`0`) en el template, sin estado reactivo conectado a ningún store o servicio.

---

## Implementation plan

1. Reemplazar el contenido de `app/components/AppLogo.vue`: quitar el SVG inline y usar `<img src="/logo.png" alt="Skinet" class="w-auto h-6 shrink-0" />`.
2. En `app/layouts/default.vue`, agregar los links "Shop" y "Contact" junto al link "Home" existente en el slot `#left` del header.
3. Crear `app/pages/shop.vue` y `app/pages/contact.vue` como placeholders mínimos.
4. En `app/layouts/default.vue`, reemplazar el `<div />` reservado para el carrito por un ícono (`i-lucide-shopping-cart` o equivalente) envuelto en `UChip` (o similar) mostrando el badge fijo en `0`.
5. Agregar un ícono de usuario (`i-lucide-circle-user` o equivalente) con `UDropdownMenu` con dos ítems: "Login" (→ `/login`) y "Register" (→ `/register`), en el slot `#right` del header.
6. Crear `app/pages/login.vue` y `app/pages/register.vue` como placeholders mínimos.
7. Verificación manual: `bun`/`pnpm dev` (a cargo del usuario) — revisar visualmente el header completo (logo real, 3 links de nav, carrito con badge, dropdown de usuario) y que no haya errores de consola.

---

## Acceptance criteria

- [x] `AppLogo.vue` muestra el logo real (`public/logo.png`) en vez del SVG placeholder.
- [x] El header muestra tres links de navegación: Home, Shop, Contact — cada uno navega a su ruta real.
- [x] `app/pages/shop.vue` y `app/pages/contact.vue` existen como placeholders mínimos.
- [x] El header muestra un ícono de carrito con un badge visible mostrando `0`.
- [x] El header muestra un ícono de usuario que, al hacer clic, despliega un dropdown con "Login" y "Register".
- [x] "Login" navega a `/login` y "Register" navega a `/register`; ambas páginas existen como placeholders mínimos.
- [x] Ni el carrito ni el dropdown de usuario hacen llamadas a ninguna API — son puramente visuales.
- [x] La app levanta sin errores en consola del navegador ni del servidor de desarrollo.

---

## Decisions

- **Sí:** usar `<img>` apuntando a `public/logo.png` en vez de inline el PNG en el componente. Es la forma estándar de servir un asset estático en Nuxt y evita procesar un binario como si fuera markup.
- **No:** conectar el carrito a `CartService`/Redis en esta spec, aunque el backend ya existe. Mezclaría estructura visual con integración de datos; queda para una spec futura de "carrito funcional".
- **Sí:** crear páginas placeholder reales (`/shop`, `/contact`, `/login`, `/register`) en vez de links sin destino (`#`), para que la navegación del header sea verificable end-to-end aunque el contenido esté vacío.
- **No:** implementar formularios ni lógica de Login/Register. No existe backend de autenticación en el proyecto todavía; esto es solo la entrada visual al flujo futuro.
- **No:** agregar menú responsive/hamburguesa para mobile. No se discutió y añade complejidad de interacción fuera del alcance de esta spec.

---

## What is **not** in this spec

- Lógica funcional del carrito (contador real, agregar/quitar productos).
- Autenticación real (backend y frontend de Login/Register).
- Contenido real de Shop y Contact.
- Navegación responsive para mobile.

Cada uno de estos, si se implementa, va en su propio spec.
