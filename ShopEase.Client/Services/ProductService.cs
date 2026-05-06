using ShopEase.Shared.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;


namespace ShopEase.Client.Services
{
    public class ProductService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly UserService _userService;


public ProductService(HttpClient http, ILocalStorageService localStorage, UserService userService)
{
    _http = http;
    _localStorage = localStorage;
    _userService = userService;
}
        // Get all products
        public async Task<List<Product>> GetProductsAsync() =>
            await _http.GetFromJsonAsync<List<Product>>("api/products") ?? new();

        // Get product by id
        public async Task<Product?> GetProductByIdAsync(int id) =>
            await _http.GetFromJsonAsync<Product>($"api/products/{id}");

        // Admin-only: Add product
       public async Task<Product?> AddProductAsync(Product product)
{
    // Retrieve JWT from localStorage
    var token = await _localStorage.GetItemAsync<string>("jwt");
    if (!string.IsNullOrEmpty(token))
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    Console.WriteLine($"Token: {token}");

    // Send request with or without token
    var response = await _http.PostAsJsonAsync("api/products", product);

     if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        // Delegate refresh to UserService
        var refreshed = await _userService.RefreshJwtAsync();
        if (refreshed)
        {
            // Retry original request
            response = await _http.PostAsJsonAsync("api/products", product);
        }
    }

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    return await response.Content.ReadFromJsonAsync<Product>();
}


        // Admin-only: Update product
        public async Task<bool> UpdateProductAsync(Product product)
        {
            
            // Attach JWT
            var token = await _localStorage.GetItemAsync<string>("jwt");
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            }
            var response = await _http.PutAsJsonAsync($"api/products/{product.ProductId}", product);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        // Delegate refresh to UserService
        var refreshed = await _userService.RefreshJwtAsync();
        if (refreshed)
        {
            response = await _http.PutAsJsonAsync($"api/products/{product.ProductId}", product);
        }
    }
            
            return response.IsSuccessStatusCode;
        }

        // Admin-only: Delete product
        public async Task<bool> DeleteProductAsync(int id)
        {

            // Attach JWT
    var token = await _localStorage.GetItemAsync<string>("jwt");
    if (!string.IsNullOrEmpty(token))
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
    
            var response = await _http.DeleteAsync($"api/products/{id}");
            
             if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        // Delegate refresh to UserService
        var refreshed = await _userService.RefreshJwtAsync();
        if (refreshed)
        {
            response = await _http.DeleteAsync($"api/products/{id}");
        }
    }
            
            return response.IsSuccessStatusCode;
        }
    }
}
