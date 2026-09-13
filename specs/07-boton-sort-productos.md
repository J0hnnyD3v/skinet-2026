# SPEC 07 — Botón de ordenar productos (Sort)

> **Status:** Implementada
> **Depends on:** SPEC 06
> **Date:** 2026-09-13
> **Objective:** Agregar un botón "Sort" junto a "Filters" en la home, con un dropdown de selección única (Alphabetical / Price: Low-High / Price: High-Low) que reordena el grid usando el parámetro `sort` que el backend ya soporta.

---

## Por qué existe este spec

`ProductController.GetProducts`/`ProductRepository.GetProductsAsync` ya aceptan un parámetro `sort` (`"priceAsc"`, `"priceDesc"`, o el default que ordena por nombre) desde SPEC 01 del backend original, pero nada en el frontend lo usa todavía. El usuario compartió un mockup de un botón "Sort" con un dropdown de tres opciones de selección única, siguiendo el mismo patrón visual que el botón "Filters" agregado en SPEC 06.

---

## Scope

**In:**

- Botón "Sort" (ícono de flechas arriba/abajo, ej. `i-lucide-arrow-up-down`) ubicado junto al botón "Filters" en `app/pages/index.vue`, con el mismo tratamiento visual (`variant="subtle"`, `size="lg"`, mismo radius/sombra definidos en SPEC 06).
- Dropdown de selección única con tres opciones: **Alphabetical**, **Price: Low-High**, **Price: High-Low**. La opción activa se distingue visualmente (ej. ícono de check o indicador de selección).
- Elegir una opción aplica el sort de inmediato (sin botón "Apply" separado, a diferencia del modal de filtros de SPEC 06) y refresca el grid.
- `useProducts.ts` acepta el sort seleccionado (reactivo) y lo manda como `sort: 'priceAsc' | 'priceDesc' | undefined` en la query a `GET /api/product`, combinándose con los filtros de brand/type ya existentes de SPEC 06.
- Sort por defecto al cargar la home: **Alphabetical** (equivalente a no mandar `sort`, mismo comportamiento que hoy sin este spec).

**Out of scope (para specs futuros):**

- Cambios al backend — `sort` ya existe y soporta exactamente estos tres casos (`priceAsc`/`priceDesc`/default).
- Persistir el sort seleccionado en la URL o entre sesiones — vive en memoria del componente, igual que los filtros de SPEC 06.
- Combinar sort con paginación visible — sigue trayendo una sola página de hasta 10 resultados.
- Cualquier otro criterio de orden (ej. por marca, por stock) no contemplado en el mockup.

---

## Data model

No se agregan estructuras nuevas de datos ni cambios de contrato en el backend. Estado local (no persistido) en `app/pages/index.vue`:

```ts
type ProductSort = 'priceAsc' | 'priceDesc' | undefined // undefined = Alphabetical (default)
```

---

## Implementation plan

1. **Frontend — estado de sort:** en `app/pages/index.vue`, agregar un `ref<ProductSort>(undefined)` para el sort seleccionado.
2. **Frontend — botón y dropdown:** agregar el `UButton` "Sort" junto a "Filters", con un dropdown (`UDropdownMenu` u otro mecanismo de selección única de Nuxt UI) con las tres opciones; seleccionar una actualiza el `ref` de sort inmediatamente.
3. **Frontend — fetch:** actualizar `useProducts.ts` para aceptar el sort (reactivo) y agregarlo a la `query` computada de `useFetch` junto a `brand`/`type`/`pageSize`, solo incluyendo `sort` en la request cuando no es `undefined`.
4. **Verificación manual** (a cargo del usuario): abrir el dropdown, elegir cada una de las tres opciones y confirmar que el grid se reordena (alfabético / precio ascendente / precio descendente), que la opción activa se ve marcada, y que combinar sort con filtros de SPEC 06 sigue funcionando sin errores de consola.

---

## Acceptance criteria

- [x] Aparece un botón "Sort" junto a "Filters" en la home, con el mismo estilo visual definido en SPEC 06.
- [x] Al hacer clic se despliega un dropdown con las tres opciones: Alphabetical, Price: Low-High, Price: High-Low.
- [x] La opción actualmente seleccionada se distingue visualmente de las otras dos.
- [x] Elegir "Price: Low-High" reordena el grid de menor a mayor precio (verificable en Network: `sort=priceAsc`).
- [x] Elegir "Price: High-Low" reordena el grid de mayor a menor precio (verificable en Network: `sort=priceDesc`).
- [x] Elegir "Alphabetical" reordena el grid por nombre y la request no manda `sort` (o lo manda vacío, igual al comportamiento default de hoy).
- [x] El sort se puede combinar con los filtros de brand/type de SPEC 06 sin romper ninguno de los dos.
- [x] La app levanta sin errores de consola en ningún estado.

---

## Decisions

- **Sí:** no tocar el backend — `sort` ya soporta exactamente los tres casos necesarios.
- **Sí:** aplicar el sort de inmediato al seleccionar una opción (sin botón "Apply"), a diferencia del modal de filtros. El usuario lo pidió así explícitamente y es coherente con el mockup (un dropdown de radio buttons, no un modal con acción diferida).
- **No:** persistir el sort en la URL o entre sesiones. Mismo criterio que los filtros de SPEC 06.
- **No:** agregar más criterios de orden además de los tres del mockup.
- **Desvíos surgidos durante la implementación (no anticipados en el plan):**
  - En vez de marcar la opción activa con un ícono que aparece/desaparece (causaba un salto visual entre ítems), se usa el estado `active` nativo de `UDropdownMenu` — resalta el ítem seleccionado con fondo y texto `primary`, sin mover el resto del layout. Cada opción tiene además su propio ícono distintivo (A-Z, flecha ascendente/descendente) en vez de compartir uno solo.
  - Se agregó un ítem `type: 'label'` ("Sort by") como encabezado no seleccionable del dropdown, y se ajustó el ancho (`w-56`), tamaño (`size="lg"`) y padding (`py-2.5` por ítem) tras varias rondas de feedback visual del usuario ("se ve muy feo", "puede mejorar", "un poco de espacio entre las opciones").
  - Durante la implementación apareció un error transitorio de HMR de Vite al cambiar la firma de `useProducts` en caliente (módulo stale) — no era un bug real, se resolvió con un hard-reload del navegador.

---

## What is **not** in this spec

- Cambios al backend de ordenamiento.
- Persistencia del sort en la URL o entre sesiones.
- Otros criterios de orden no contemplados en el mockup.

Cada uno de estos, si se implementa, va en su propio spec.
