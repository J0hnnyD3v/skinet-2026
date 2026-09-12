using Microsoft.AspNetCore.Mvc;

using API.Dtos;
using API.Dtos.Products;
using API.Errors;
using API.Extensions;
using Core.Entities;
using Core.Interfaces;

namespace API.Controllers;

public class ProductController(IProductRepository repository) : BaseApiController
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult> GetProducts(string? brand, string? type, string? sort, string? search, int pageIndex = 1, int pageSize = 10)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (products, count) = await repository.GetProductsAsync(brand, type, sort, search, pageIndex, pageSize);

        // Select == .map() de JS/TS, pero perezoso: no recorre nada acá, se ejecuta cuando el
        // serializador JSON consume la secuencia. ToDto es método de extensión (API/Extensions/
        // ProductMappings.cs), de ahí el "using API.Extensions".
        var dtos = products.Select(p => p.ToDto()).ToList();

        return ApiOk(new Pagination<ProductDto>(pageIndex, pageSize, count, dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetProductById(int id)
    {
        var product = await repository.GetProductByIdAsync(id);

        if (product == null) return ApiError(StatusCodes.Status404NotFound, "Product not found", ErrorCodes.Product.NotFound);

        return ApiOk(product.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            PictureUrl = dto.PictureUrl,
            Type = dto.Type,
            Brand = dto.Brand,
            QuantityInStock = dto.QuantityInStock
        };

        repository.AddProduct(product);

        if (await repository.SaveChangesAsync())
        {
            return ApiCreated(product.ToDto(), nameof(GetProductById), new { id = product.Id }, "Product created successfully");
        }

        return ApiError(StatusCodes.Status400BadRequest, "Problem creating the product", ErrorCodes.Product.CreateError);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        if (dto.Id != id) return ApiError(StatusCodes.Status400BadRequest, "Product ID mismatch", ErrorCodes.Product.IdMismatch);

        if (!repository.ProductExists(id)) return ApiError(StatusCodes.Status404NotFound, "Product not found", ErrorCodes.Product.NotFound);

        var product = new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            PictureUrl = dto.PictureUrl,
            Type = dto.Type,
            Brand = dto.Brand,
            QuantityInStock = dto.QuantityInStock
        };

        repository.UpdateProduct(product);

        if (await repository.SaveChangesAsync())
        {
            return ApiOk(product.ToDto(), "Product updated successfully");
        }

        return ApiError(StatusCodes.Status400BadRequest, "Problem updating the product", ErrorCodes.Product.UpdateError);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await repository.GetProductByIdAsync(id);

        if (product == null) return ApiError(StatusCodes.Status404NotFound, "Product not found", ErrorCodes.Product.NotFound);

        repository.DeleteProduct(product);

        if (await repository.SaveChangesAsync())
        {
            return ApiOk(product.ToDto(), "Product deleted successfully");
        }

        return ApiError(StatusCodes.Status400BadRequest, "Problem deleting the product", ErrorCodes.Product.DeleteError);
    }

    [HttpGet("brands")]
    public async Task<ActionResult> GetBrands()
    {
        var brands = await repository.GetBrandsAsync();

        return ApiOk(brands);
    }

    [HttpGet("types")]
    public async Task<ActionResult> GetTypes()
    {
        var types = await repository.GetTypesAsync();

        return ApiOk(types);
    }
}
