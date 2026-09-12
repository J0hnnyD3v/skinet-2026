<template>
  <UHeader>
    <template #left>
      <NuxtLink
        to="/"
        class="focus-visible:outline-3 outline-primary/25 rounded-md p-1 -ms-1"
      >
        <AppLogo class="w-auto h-6 shrink-0" />
      </NuxtLink>

      <NuxtLink to="/" class="text-sm font-medium">
        Home
      </NuxtLink>
    </template>

    <template #right>
      <!-- Espacio reservado para el ícono de carrito (spec futuro) -->
      <div />

      <UColorModeButton />
    </template>
  </UHeader>

  <UMain>
    <slot />
  </UMain>

  <USeparator icon="i-simple-icons-nuxtdotjs" />

  <UFooter>
    <template #left>
      <p class="text-sm text-muted">
        © Skinet {{ new Date().getFullYear() }}
      </p>
    </template>
  </UFooter>
</template>
