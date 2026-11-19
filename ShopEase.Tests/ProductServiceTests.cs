using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ShopEase.Server.Data;
using ShopEase.Server.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ShopEase.Tests;

public class ProductServiceTests
{
    private static ShopDbContext CreateContext() 
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>() 
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
            .Options;

        return new ShopDbContext(options);
    }

    private static ProductService CreateService(
        ShopDbContext context) 
    {
        var cache = new MemoryCache(
            new MemoryCacheOptions());

        var redisCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));

        return new ProductService(
            context,
            cache,
            redisCache,
            NullLogger<ProductService>.Instance);
    }

    [Fact] 
    public async Task DeleteProductAsync_ReturnsFalse_WhenProductDoesNotExist() 
    {
        // Arrange

        await using var context = CreateContext();

        var service = CreateService(context);

        // Act

        var result =
            await service.DeleteProductAsync(999);

        // Assert

        Assert.False(result); 
    }
}