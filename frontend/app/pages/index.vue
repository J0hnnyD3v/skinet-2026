<script setup lang="ts">
const { products, pending, error } = useProducts()
</script>

<template>
  <UContainer class="max-w-none py-8">
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
  </UContainer>
</template>
