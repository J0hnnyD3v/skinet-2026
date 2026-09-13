<script setup lang="ts">
import type { ProductFilters } from '~/types/product-filters'

const filters = ref<ProductFilters>({ brands: [], types: [] })
const filtersModalOpen = ref(false)

const { products, pending, error } = useProducts(filters)

function applyFilters(newFilters: ProductFilters) {
  filters.value = newFilters
}
</script>

<template>
  <UContainer class="max-w-none py-8">
    <div class="flex justify-end mb-6">
      <UButton
        icon="i-lucide-sliders-horizontal"
        color="primary"
        variant="subtle"
        size="lg"
        class="rounded-lg font-semibold shadow-sm hover:shadow-md transition-shadow"
        @click="filtersModalOpen = true"
      >
        Filters
      </UButton>
    </div>

    <ProductFiltersModal
      v-model:open="filtersModalOpen"
      :filters="filters"
      @apply="applyFilters"
    />

    <ClientOnly>
      <p
        v-if="error"
        class="text-muted"
      >
        No se pudieron cargar los productos.
      </p>

      <div
        v-else
        class="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-6"
      >
        <template v-if="pending">
          <USkeleton
            v-for="n in 10"
            :key="n"
            class="h-72 w-full"
          />
        </template>

        <ProductCard
          v-for="product in products"
          v-else
          :key="product.id"
          :product="product"
        />
      </div>

      <template #fallback>
        <div class="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-6">
          <USkeleton
            v-for="n in 10"
            :key="n"
            class="h-72 w-full"
          />
        </div>
      </template>
    </ClientOnly>
  </UContainer>
</template>
