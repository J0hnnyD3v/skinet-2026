# SPEC 05 — Estilos de header y home (paleta, layout, jerarquía visual y responsive)

> **Status:** Implementada
> **Depends on:** SPEC 02, SPEC 03
> **Date:** 2026-09-12
> **Objective:** Rediseñar visualmente el header y la home — paleta azul, ancho completo, mayor jerarquía en las cards — y resolver el menú mobile (hamburguesa + `USlideover`) que SPEC 02 había dejado pendiente.

---

## Por qué existe este spec

El header y la home funcionan (SPEC 01–04 ya resolvieron estructura, navegación, datos reales e imágenes), pero visualmente se sienten genéricos: paleta verde sin relación con la marca ("Shinet-Core Snow Sports Gear"), header y grid de home centrados con espacio vacío a los lados en pantallas anchas, cards planas sin jerarquía, y sin ningún tratamiento para mobile (SPEC 02 dejó el menú hamburguesa explícitamente fuera de su alcance). Este spec cierra esos puntos de una vez, a pedido explícito del usuario, aunque normalmente se hubiera dividido en "estética desktop" y "responsive" por separado.

---

## Scope

**In:**

- **Paleta:** cambiar `primary` de `'green'` a `'blue'` en `app/app.config.ts`. `neutral` se mantiene en `'slate'`.
- **Header — ancho completo:** el `UHeader` deja de estar limitado por un contenedor centrado con max-width; su contenido (logo, nav, iconos) se extiende a todo el ancho del viewport, con padding lateral responsive (ej. `px-4 sm:px-6 lg:px-8`) en vez de márgenes automáticos centrados.
- **Header — espaciado:** aumentar el padding vertical del header (se siente muy compacto hoy) y el espacio/gap entre el logo y los links de navegación.
- **Header — responsive (mobile, breakpoint `sm` de Tailwind, <640px):** los links Home/Shop/Contact se ocultan en la barra bajo ese breakpoint; aparece un botón de hamburguesa (ícono `i-lucide-menu`) que abre un `USlideover` con esos tres links en columna. El carrito, el ícono de usuario y el toggle de tema (`UColorModeButton`) permanecen siempre visibles en la barra, en cualquier tamaño de viewport.
- **Home — ancho completo:** la home deja de estar limitada por un `UContainer` centrado; el grid de cards se extiende a todo el ancho del viewport, con el mismo padding lateral responsive que el header.
- **Home — grid:** aumentar el `gap` entre cards (más espacio del que dejó SPEC 03).
- **Cards — jerarquía visual:** en `ProductCard.vue`, agregar sombra (`shadow-md`, con `hover:shadow-lg` y transición) para que la card resalte sobre el fondo; aumentar el peso/tamaño del nombre del producto y del precio; dejar los badges de marca/tipo con un tratamiento más sutil que el texto principal (menor contraste/tamaño) para no competir visualmente con nombre y precio.

**Out of scope (para specs futuros):**

- Rediseño del logo (`AppLogo.vue`) — colores y forma del logo actual se mantienen igual.
- Página de detalle de producto y su navegación.
- Filtros, búsqueda, orden o paginación visible en la home.
- Contenido real de `/shop` y `/contact`.
- Lógica funcional de carrito/autenticación (los iconos siguen siendo visuales, sin conexión a `CartService`/backend de auth).
- Rediseño de `AppLogo.vue`, footer, o cualquier página fuera de header/home.

---

## Data model

Esta spec no introduce estructuras de datos nuevas. Es puramente de estilos/layout (CSS/Tailwind, clases de Nuxt UI, `app.config.ts`).

---

## Implementation plan

1. **Paleta:** en `app/app.config.ts`, cambiar `ui.colors.primary` de `'green'` a `'blue'`.
2. **Header ancho completo + espaciado:** en `app/layouts/default.vue`, quitar el max-width/centrado del `UHeader` (vía su prop `ui`/`container`, o reestructurando el markup si Nuxt UI no expone esa opción directamente) para que ocupe el 100% del ancho con padding lateral responsive; aumentar el padding vertical del header y el gap entre `AppLogo` y los links de navegación.
3. **Header responsive:** en `app/layouts/default.vue`, envolver Home/Shop/Contact en un contenedor oculto bajo `sm` (`hidden sm:flex`); agregar un `UButton` con ícono de hamburguesa visible solo bajo `sm` (`sm:hidden`) que abra un `USlideover` con esos tres links en columna. Verificar que carrito, usuario y `UColorModeButton` no se ven afectados por este cambio y siguen visibles en todos los tamaños.
4. **Home ancho completo + gap:** en `app/pages/index.vue`, quitar el `UContainer` centrado (o convertirlo en un wrapper de ancho completo con el mismo padding lateral que el header) y aumentar el `gap` del grid de `ProductCard`.
5. **Cards con jerarquía:** en `ProductCard.vue`, agregar `shadow-md hover:shadow-lg transition-shadow` a `UCard`; aumentar tamaño/peso de `product.name` (ej. `text-lg font-semibold`) y del precio (ej. `text-xl font-bold`); reducir el énfasis visual de los badges de marca/tipo (ej. `variant="soft"` + texto más pequeño) para que no compitan con nombre/precio.
6. **Verificación manual** (a cargo del usuario): en desktop, confirmar que header y home ocupan todo el ancho del viewport, con la paleta azul, y que las cards tienen sombra y jerarquía visual clara; reducir el viewport a menos de 640px y confirmar que el nav colapsa a un ícono de hamburguesa que abre el `USlideover` con los tres links funcionando, sin errores de consola en ningún tamaño.

---

## Acceptance criteria

- [x] `app/app.config.ts` tiene `primary: 'blue'`.
- [x] En desktop, el header ocupa el 100% del ancho del viewport (sin franjas vacías laterales de un contenedor centrado).
- [x] El header tiene más padding vertical y más espacio entre logo y nav que antes de este spec.
- [x] En viewport menor a 640px, Home/Shop/Contact no aparecen directamente en la barra del header; en su lugar hay un ícono de hamburguesa.
- [x] Al hacer clic en la hamburguesa se abre un `USlideover` con los tres links de navegación, y cada uno navega a su ruta correcta.
- [x] El carrito, el ícono de usuario y el toggle de tema siguen visibles en la barra del header en cualquier tamaño de viewport (desktop y mobile).
- [x] En desktop, la home ocupa el 100% del ancho del viewport (sin `UContainer` centrado limitando el grid).
- [x] El grid de cards tiene más espacio (`gap`) entre elementos que el definido en SPEC 03.
- [x] Cada `ProductCard` muestra una sombra visible que se intensifica al pasar el mouse (`hover`), y el nombre/precio tienen visualmente más peso que los badges de marca/tipo.
- [x] La app levanta sin errores de consola en ningún tamaño de viewport probado.

---

## Decisions

- **Sí:** cambiar `primary` a azul en vez de mantener verde. Encaja con la temática "snow sports" del logo (`Shinet-Core Snow Sports Gear`); decisión explícita del usuario.
- **Sí:** meter paleta + layout de header + layout de home + jerarquía de cards + responsive en un solo spec, a pesar de tocar más de tres dominios (lo que normalmente ameritaría dividirlo en dos). El usuario decidió explícitamente no dividirlo tras la advertencia.
- **Sí:** usar `USlideover` para el menú mobile en vez de un `UDropdownMenu`. Deja espacio para contenido futuro (categorías, filtros) en el panel lateral, y es el patrón que Nuxt UI recomienda para navegación mobile más allá de 2-3 ítems.
- **No:** rediseñar `AppLogo.vue`. No se discutió y el logo actual ya tiene su propia paleta de color (azul/rosa) que no depende de `primary`/`neutral` de Nuxt UI.
- **No:** tocar la lógica del carrito/usuario (siguen siendo visuales, badge fijo en 0, dropdown de Login/Register). Fuera del alcance de "estilos".
- **No:** cambiar los breakpoints del grid de home definidos en SPEC 03 (1/2/4 columnas) — solo se ajusta el `gap` y el ancho del contenedor, no la cantidad de columnas por breakpoint.
- **Desvíos surgidos durante la implementación (no anticipados en el plan):**
  - El `USlideover` del menú mobile se construyó a mano (`ref` + `UButton` + `USlideover` propios) en vez de usar el modo `mode="slideover"` nativo de `UHeader`, porque ese mecanismo pone el nav en un slot `center` que se renderiza centrado en la barra — el usuario pidió el nav pegado al logo a la izquierda, no centrado. También se agregó `:toggle="false"` a `UHeader` para apagar su botón de hamburguesa nativo (quedaba un segundo ícono duplicado sin usar).
  - El grid de home se implementó con `grid-cols-[repeat(auto-fill,minmax(220px,1fr))]` en vez de breakpoints fijos, para que aparezcan más cards por fila en pantallas anchas sin agrandar cada card — más ajustado a "aprovechar el espacio" que lo que decía el plan original.
  - Se detectó y arregló un bug no relacionado a estilos: con el `prerender` quitado en SPEC 03, el fetch de productos corre también en el servidor de Nuxt (SSR) vía Node, que rechaza el certificado autofirmado de `https://localhost:7075` en dev (`DEPTH_ZERO_SELF_SIGNED_CERT`), aunque el navegador sí lo acepte — causaba "No se pudieron cargar los productos" en un hard-reload. Se resolvió agregando `server: false` a `useFetch` en `useProducts.ts`, para que el fetch corra solo en el navegador.
  - El shadow de las cards se ajustó para dark mode: un `box-shadow` gris no se distingue sobre fondo oscuro, así que en `dark:` se usa un shadow teñido de `primary` (azul) en vez de gris, además del `ring` al hover.

---

## Identified risks

- La forma exacta de quitar el max-width/centrado de `UHeader` y `UContainer` depende de qué props expone la versión instalada de `@nuxt/ui` (`^4.11.0`) para su contenedor interno (`ui.container` u otra). Si el componente no permite anular ese contenedor directamente vía prop, puede requerir reestructurar el markup (ej. no usar `UContainer`/wrapper por defecto y armar el padding a mano) — se resuelve durante la implementación, no bloquea el spec.

---

## What is **not** in this spec

- Rediseño del logo.
- Página de detalle de producto.
- Filtros, búsqueda, orden y paginación visible en la home.
- Contenido real de Shop y Contact.
- Lógica funcional de carrito y autenticación.

Cada uno de estos, si se implementa, va en su propio spec.
