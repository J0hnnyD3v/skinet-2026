using System.Text.Json;
using Core.Entities;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context)
    {
        if (!context.Products.Any())
        {
            var productsFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "products.json");
            var productsData = await File.ReadAllTextAsync(productsFilePath);

            var products = JsonSerializer.Deserialize<List<Product>>(productsData);

            if (products == null)
            {
                return;
            }

            context.Products.AddRange(products);

            await context.SaveChangesAsync();
        }
    }
}