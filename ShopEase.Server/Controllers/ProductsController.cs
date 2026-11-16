using Microsoft.AspNetCore.Mvc;       
using Microsoft.EntityFrameworkCore;  
using ShopEase.Server.Data;           
using ShopEase.Shared.Models;         
using Microsoft.AspNetCore.Authorization; 
using ShopEase.Shared.DTOs;
using Microsoft.Extensions.Caching.Memory;
using ShopEase.Server.Services;

namespace ShopEase.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {        
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // -------------------------
        // Public: Get all products
        // -------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {  
            var products = await _productService.GetProductsAsync();
            return Ok(products);
        }

        // -------------------------
        // Public: Get product by id
        // -------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok (product);
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

           product = await _productService.AddProductAsync(product);

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
             var updatedProduct =
            await _productService.UpdateProductAsync(product);
                if (updatedProduct == null)
                return NotFound();
            
            return NoContent();     // in the future we can change into : return Ok(updatedProduct);
        }

        // -------------------------
        // Admin-only: Delete product
        // -------------------------
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
