<script setup lang="ts">
import type { Product } from '~/types/product'

const { product } = defineProps<{ product: Product }>()

const imageFailed = ref(false)

const config = useRuntimeConfig()

const imageUrl = computed(() => {
  const apiOrigin = config.public.apiBase.replace(/\/api\/?$/, '')
  return `${apiOrigin}${product.pictureUrl}`
})

const formattedPrice = computed(() =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(product.price)
)
</script>

<template>
  <UCard class="shadow-md hover:shadow-lg dark:shadow-lg dark:shadow-primary/10 dark:hover:shadow-primary/30 dark:hover:ring-2 dark:hover:ring-primary/60 transition-all">
    <template #header>
      <div class="aspect-square flex items-center justify-center bg-muted rounded-md overflow-hidden">
        <img
          v-if="!imageFailed"
          :src="imageUrl"
          :alt="product.name"
          class="w-full h-full object-cover"
          @error="imageFailed = true"
        >
        <UIcon
          v-else
          name="i-lucide-image-off"
          class="size-12 text-muted"
        />
      </div>
    </template>

    <div class="flex flex-col gap-2">
      <h3 class="text-lg font-semibold truncate">
        {{ product.name }}
      </h3>

      <div class="flex gap-1 flex-wrap">
        <UBadge
          color="neutral"
          variant="soft"
          size="sm"
        >
          {{ product.brand }}
        </UBadge>
        <UBadge
          color="neutral"
          variant="soft"
          size="sm"
        >
          {{ product.type }}
        </UBadge>
      </div>

      <p class="text-xl font-bold">
        {{ formattedPrice }}
      </p>

      <UButton
        icon="i-lucide-shopping-cart"
        color="primary"
        size="lg"
        block
        class="font-semibold rounded-lg shadow-sm hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0 transition-all"
      >
        Add to cart
      </UButton>
    </div>
  </UCard>
</template>
