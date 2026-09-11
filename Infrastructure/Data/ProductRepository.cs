using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ProductRepository(StoreContext context) : IProductRepository
{
    public void AddProduct(Product product)
    {
        context.Products.Add(product);
    }

    public void DeleteProduct(Product product)
    {
        context.Products.Remove(product);
    }

    public async Task<IReadOnlyList<string>> GetBrandsAsync()
    {
        return await context.Products.Select(x => x.Brand)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await context.Products.FindAsync(id);
    }

    public async Task<(IReadOnlyList<Product> Items, int Count)> GetProductsAsync(string? brand, string? type, string? sort, int pageIndex, int pageSize)
    {
        var query = context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(x => x.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Type == type);
        }

        // El conteo se calcula sobre lo ya filtrado (brand/type) pero antes de paginar,
        // porque es el total de resultados que existen para esos filtros, no solo de la página actual.
        var count = await query.CountAsync();

        query = sort switch
        {
            "priceAsc" => query.OrderBy(x => x.Price),
            "priceDesc" => query.OrderByDescending(x => x.Price),
            _ => query.OrderBy(x => x.Name)
        };

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, count);
    }

    public async Task<IReadOnlyList<string>> GetTypesAsync()
    {
        return await context.Products.Select(x => x.Type)
            .Distinct()
            .ToListAsync();
    }

    public bool ProductExists(int id)
    {
        return context.Products.Any(x => x.Id == id);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public void UpdateProduct(Product product)
    {
        context.Entry(product).State = EntityState.Modified;
    }
}