using System.Text.Json;
using StackExchange.Redis;

using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Services;

public class CartService(IConnectionMultiplexer redis) : ICartService
{
    private static readonly TimeSpan CartExpiration = TimeSpan.FromDays(30);

    private readonly IDatabase _database = redis.GetDatabase();
    public async Task<bool> DeleteCartAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<ShoppingCart?> GetCartAsync(string key)
    {
        var data = await _database.StringGetAsync(key);
        return data.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ShoppingCart>((byte[])data!);
    }

    public async Task<ShoppingCart?> SetCartAsync(ShoppingCart cart)
    {
        var json = JsonSerializer.Serialize(cart);
        var created = await _database.StringSetAsync(cart.Id, json, CartExpiration);
        return created ? cart : null;
    }
}