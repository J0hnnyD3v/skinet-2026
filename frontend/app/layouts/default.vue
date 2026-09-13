<script setup lang="ts">
const mobileNavOpen = ref(false)
</script>

<template>
  <section>
    <UHeader
      :toggle="false"
      :ui="{ container: 'max-w-none' }"
    >
      <template #left>
        <NuxtLink
          to="/"
          class="focus-visible:outline-3 outline-primary/25 rounded-md p-1 -ms-1"
        >
          <AppLogo />
        </NuxtLink>

        <nav class="hidden sm:flex items-center gap-6">
          <NuxtLink
            to="/"
            class="text-sm font-medium"
          >
            Home
          </NuxtLink>

          <NuxtLink
            to="/shop"
            class="text-sm font-medium"
          >
            Shop
          </NuxtLink>

          <NuxtLink
            to="/contact"
            class="text-sm font-medium"
          >
            Contact
          </NuxtLink>
        </nav>

        <UButton
          icon="i-lucide-menu"
          aria-label="Abrir menú"
          color="neutral"
          variant="ghost"
          class="sm:hidden"
          @click="mobileNavOpen = true"
        />
      </template>

      <template #right>
        <UChip
          text="0"
          size="3xl"
        >
          <UButton
            icon="i-lucide-shopping-cart"
            aria-label="Carrito"
            color="neutral"
            variant="ghost"
          />
        </UChip>

        <UDropdownMenu
          :items="[[
            { label: 'Login', to: '/login' },
            { label: 'Register', to: '/register' }
          ]]"
        >
          <UButton
            icon="i-lucide-circle-user"
            aria-label="Usuario"
            color="neutral"
            variant="ghost"
          />
        </UDropdownMenu>

        <UColorModeButton />
      </template>
    </UHeader>

    <USlideover
      v-model:open="mobileNavOpen"
      title="Menú"
    >
      <template #body>
        <div class="flex flex-col gap-4">
          <NuxtLink
            to="/"
            class="text-sm font-medium"
            @click="mobileNavOpen = false"
          >
            Home
          </NuxtLink>

          <NuxtLink
            to="/shop"
            class="text-sm font-medium"
            @click="mobileNavOpen = false"
          >
            Shop
          </NuxtLink>

          <NuxtLink
            to="/contact"
            class="text-sm font-medium"
            @click="mobileNavOpen = false"
          >
            Contact
          </NuxtLink>
        </div>
      </template>
    </USlideover>

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
  </section>
</template>
