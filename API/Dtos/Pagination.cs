namespace API.Dtos;

/// <summary>
/// Envoltorio de presentación para respuestas paginadas. No es un DTO de un recurso
/// específico (por eso vive en API/Dtos y no en API/Dtos/Products) — cualquier lista
/// paginada de la API puede reutilizarlo.
/// </summary>
public class Pagination<T>(int pageIndex, int pageSize, int count, IReadOnlyList<T> items)
{
    public int PageIndex { get; set; } = pageIndex;
    public int PageSize { get; set; } = pageSize;
    public int Count { get; set; } = count;
    public IReadOnlyList<T> Items { get; set; } = items;
}
