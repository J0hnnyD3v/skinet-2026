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
  <UCard>
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
      <h3 class="font-medium truncate">
        {{ product.name }}
      </h3>

      <div class="flex gap-1 flex-wrap">
        <UBadge
          color="neutral"
          variant="subtle"
        >
          {{ product.brand }}
        </UBadge>
        <UBadge
          color="neutral"
          variant="subtle"
        >
          {{ product.type }}
        </UBadge>
      </div>

      <p class="font-semibold">
        {{ formattedPrice }}
      </p>
    </div>
  </UCard>
</template>
