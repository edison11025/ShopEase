using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShopEase.Server.Data;
using ShopEase.Shared.Models;

namespace ShopEase.Server.Services
{
    public class ProductService : IProductService
    {
        private readonly ShopDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductService> _logger;

        private const string ProductsCacheKey = "products";

        public ProductService(
            ShopDbContext context,
            IMemoryCache cache,
            ILogger<ProductService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            if (!_cache.TryGetValue(
                    ProductsCacheKey,
                    out List<Product>? products))
            {
                _logger.LogInformation(
                    "Fetching products from database.");

                products = await _context.Products
                    .AsNoTracking()      
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(
                    ProductsCacheKey,
                    products,
                    cacheOptions);
            }
            else
            {
                _logger.LogInformation(
                    "Fetching products from cache.");
            }

            return products!;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<Product> AddProductAsync(Product product)
	    {
 	        _context.Products.Add(product);
            await _context.SaveChangesAsync();

            InvalidateProductsCache();		
	    return product;
	    }

        public async Task<Product?> UpdateProductAsync(Product product)
{
    var existingProduct = await _context.Products
        .FirstOrDefaultAsync(
            p => p.ProductId == product.ProductId);

    if (existingProduct == null)
        return null;

    _context.Entry(product).State =
        EntityState.Modified;

    await _context.SaveChangesAsync();
    InvalidateProductsCache();
    return product;
}
    
    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        InvalidateProductsCache();
        return true;

    }

    private void InvalidateProductsCache()
{
    _cache.Remove(ProductsCacheKey);
    _logger.LogInformation(
        "Products cache invalidated.");
}
}
}