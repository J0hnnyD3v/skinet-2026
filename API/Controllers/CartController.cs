using Microsoft.AspNetCore.Mvc;

using API.Errors;
using Core.Entities;
using Core.Interfaces;

namespace API.Controllers;

public class CartController(ICartService cartService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult> GetCart(string key)
    {
        var cart = await cartService.GetCartAsync(key);

        if (cart == null) return ApiError(StatusCodes.Status404NotFound, "Cart not found", ErrorCodes.Cart.NotFound);

        return ApiOk(cart);
    }

    [HttpPost]
    public async Task<ActionResult> SetCart(ShoppingCart cart)
    {
        var updatedCart = await cartService.SetCartAsync(cart);

        if (updatedCart == null) return ApiError(StatusCodes.Status400BadRequest, "Problem saving the cart", ErrorCodes.Cart.SaveError);

        return ApiOk(updatedCart);
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteCart(string key)
    {
        var deleted = await cartService.DeleteCartAsync(key);

        if (!deleted) return ApiError(StatusCodes.Status404NotFound, "Cart not found", ErrorCodes.Cart.NotFound);

        return ApiOk(true, "Cart deleted successfully");
    }
}
