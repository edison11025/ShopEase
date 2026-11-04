using Microsoft.AspNetCore.Mvc;       
using Microsoft.EntityFrameworkCore;  
using ShopEase.Server.Data;           
using ShopEase.Shared.Models;         
using Microsoft.AspNetCore.Authorization; 
using ShopEase.Shared.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ShopEase.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ShopDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductsController> _logger;

        private const string ProductsCacheKey = "products";
        public ProductsController(ShopDbContext context, IMemoryCache cache, ILogger<ProductsController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // -------------------------
        // Public: Get all products
        // -------------------------
        [HttpGet]
public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
{
    

    if (!_cache.TryGetValue(ProductsCacheKey, out List<Product>? products))
    {
        _logger.LogInformation("Fetching products from database.");

        products = await _context.Products.ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

        _cache.Set(ProductsCacheKey, products, cacheOptions);
    }
    else
    {
        _logger.LogInformation("Fetching products from cache.");
    }

    return Ok(products);
}

        // -------------------------
        // Public: Get product by id
        // -------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return product;
        }

        // -------------------------
        // Admin-only: Add new product
        // -------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _cache.Remove(ProductsCacheKey); // Invalidate cache after adding a new product
            _logger.LogInformation("Product cache invalidated after product creation.");

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        // -------------------------
        // Admin-only: Update product
        // -------------------------
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            if (id != product.ProductId)
                return BadRequest();

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                _cache.Remove(ProductsCacheKey); // Invalidate cache after updating a product
                _logger.LogInformation("Product cache invalidated after product update.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.ProductId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // -------------------------
        // Admin-only: Delete product
        // -------------------------
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _cache.Remove(ProductsCacheKey); // Invalidate cache after deleting a product
            _logger.LogInformation("Product cache invalidated after product deletion.");

            return NoContent();
        }
    }
}
