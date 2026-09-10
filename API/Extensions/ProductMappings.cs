using API.Dtos.Products;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        PictureUrl = product.PictureUrl,
        Type = product.Type,
        Brand = product.Brand,
        QuantityInStock = product.QuantityInStock
    };
}
