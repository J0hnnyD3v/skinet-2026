using Core.Entities;

namespace Core.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int Count)> GetProductsAsync(string? brand, string? type, string? sort, int pageIndex, int pageSize);
    Task<Product?> GetProductByIdAsync(int id);
    Task<IReadOnlyList<string>> GetBrandsAsync();
    Task<IReadOnlyList<string>> GetTypesAsync();
    void AddProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
    bool ProductExists(int id);
    Task<bool> SaveChangesAsync();
}