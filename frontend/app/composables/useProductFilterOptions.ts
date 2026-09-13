interface ApiResponse<T> {
  statusCode: number
  message: string
  data: T
}

export function useProductFilterOptions() {
  const config = useRuntimeConfig()

  const { data: brandsResponse } = useFetch<ApiResponse<string[]>>(
    `${config.public.apiBase}/product/brands`,
    { server: false }
  )

  const { data: typesResponse } = useFetch<ApiResponse<string[]>>(
    `${config.public.apiBase}/product/types`,
    { server: false }
  )

  const brands = computed(() => brandsResponse.value?.data ?? [])
  const types = computed(() => typesResponse.value?.data ?? [])

  return { brands, types }
}
