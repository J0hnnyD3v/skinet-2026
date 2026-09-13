import type { Product } from '~/types/product'
import type { ProductFilters } from '~/types/product-filters'

interface Pagination<T> {
  pageIndex: number
  pageSize: number
  count: number
  items: T[]
}

interface ApiResponse<T> {
  statusCode: number
  message: string
  data: T
}

export function useProducts(filters: Ref<ProductFilters>) {
  const config = useRuntimeConfig()

  const { data, pending, error } = useFetch<ApiResponse<Pagination<Product>>>(
    `${config.public.apiBase}/product`,
    {
      query: computed(() => ({
        pageSize: 10,
        brand: filters.value.brands,
        type: filters.value.types
      })),
      server: false
    }
  )

  const products = computed(() => data.value?.data.items ?? [])

  return { products, pending, error }
}
