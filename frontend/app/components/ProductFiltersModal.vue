<script setup lang="ts">
import type { ProductFilters } from '~/types/product-filters'

const open = defineModel<boolean>('open', { default: false })

const { filters } = defineProps<{ filters: ProductFilters }>()

const emit = defineEmits<{
  apply: [filters: ProductFilters]
}>()

const { brands, types } = useProductFilterOptions()

const selectedBrands = ref<string[]>([...filters.brands])
const selectedTypes = ref<string[]>([...filters.types])

watch(open, (isOpen) => {
  if (isOpen) {
    selectedBrands.value = [...filters.brands]
    selectedTypes.value = [...filters.types]
  }
})

function applyFilters() {
  emit('apply', { brands: selectedBrands.value, types: selectedTypes.value })
  open.value = false
}

function clearFilters() {
  selectedBrands.value = []
  selectedTypes.value = []
  emit('apply', { brands: [], types: [] })
  open.value = false
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Filters"
    :ui="{ content: 'max-w-xl', footer: 'justify-between' }"
  >
    <template #body>
      <div class="grid grid-cols-2 gap-8">
        <div class="flex flex-col gap-3">
          <h3 class="text-xs font-semibold uppercase tracking-wide text-muted pb-2 border-b border-default">
            Brands
          </h3>

          <UCheckboxGroup
            v-model="selectedBrands"
            :items="brands"
            class="gap-3"
          />
        </div>

        <div class="flex flex-col gap-3 border-l border-default pl-8">
          <h3 class="text-xs font-semibold uppercase tracking-wide text-muted pb-2 border-b border-default">
            Types
          </h3>

          <UCheckboxGroup
            v-model="selectedTypes"
            :items="types"
            class="gap-3"
          />
        </div>
      </div>
    </template>

    <template #footer>
      <UButton
        icon="i-lucide-x"
        color="neutral"
        variant="outline"
        @click="clearFilters"
      >
        Clear
      </UButton>

      <UButton
        icon="i-lucide-check"
        color="primary"
        variant="solid"
        @click="applyFilters"
      >
        Apply Filters
      </UButton>
    </template>
  </UModal>
</template>
