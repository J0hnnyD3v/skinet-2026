using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ProductController(IProductRepository repository) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult> GetProducts(string? brand, string? type, string? sort)
    {
        var products = await repository.GetProductsAsync(brand, type, sort);

        return ApiOk(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetProductById(int id)
    {
        var product = await repository.GetProductByIdAsync(id);

        return ApiOk(product);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProduct(Product product)
    {
        repository.AddProduct(product);

        if (await repository.SaveChangesAsync())
        {
            return ApiCreated(product, nameof(GetProductById), new { id = product.Id }, "Product created successfully");
        }

        return ApiError(StatusCodes.Status400BadRequest, "Problem creating the product", ErrorCodes.Product.CreateError);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, Product product)
    {
        if (product.Id != id) return ApiError(StatusCodes.Status400BadRequest, "Product ID mismatch", ErrorCodes.Product.IdMismatch);

        if (!ProductExists(id)) return ApiError(StatusCodes.Status404NotFound, "Product not found", ErrorCodes.Product.NotFound);

        repository.UpdateProduct(product);

        if (await repository.SaveChangesAsync())
        {
            return ApiOk(product, "Product updated successfully");
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
            return ApiOk(product, "Product deleted successfully");
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

    private bool ProductExists(int id)
    {
        return repository.ProductExists(id);
    }
}