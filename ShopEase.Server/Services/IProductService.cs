using ShopEase.Shared.Models;

namespace ShopEase.Server.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> AddProductAsync(Product product);
        Task<Product?> UpdateProductAsync(Product product);

        Task<bool> DeleteProductAsync(int id);
    }
}