import type { Product } from '~/types/product'

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

export function useProducts() {
  const config = useRuntimeConfig()

  const { data, pending, error } = useFetch<ApiResponse<Pagination<Product>>>(
    `${config.public.apiBase}/product`,
    { query: { pageSize: 10 } }
  )

  const products = computed(() => data.value?.data.items ?? [])

  return { products, pending, error }
}
