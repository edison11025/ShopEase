using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ShopEase.Shared.DTOs;
using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace ShopEase.Client.Services
{
    public class CartService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;

        public event Action? OnCartChanged;

        public CartService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        // 🔎 Attach JWT
        private async Task AttachJwtAsync()
        {
            var jwtToken = await _localStorage.GetItemAsync<string>("jwt");
            if (!string.IsNullOrEmpty(jwtToken))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }
        }

        // 🔎 Refresh token if expired
        private async Task<bool> TryRefreshTokenAsync()
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var response = await _http.PostAsJsonAsync("api/user/refresh", new { RefreshToken = refreshToken });
            if (!response.IsSuccessStatusCode) return false;

            var jwtResponse = await response.Content.ReadFromJsonAsync<JwtResponse>();
            if (jwtResponse == null) return false;

            await _localStorage.SetItemAsync("jwt", jwtResponse.JwtToken);
            return true;
        }

        // 🔎 Wrapper: send request, refresh if 401, retry
        private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
        {
            await AttachJwtAsync();
            var response = await send(_http);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                bool refreshed = await TryRefreshTokenAsync();
                if (refreshed)
                {
                    await AttachJwtAsync();
                    response = await send(_http); // retry with new JWT
                }
            }

            return response;
        }

        // ✅ GET cart
        public async Task<List<CartItemResponseDto>> GetCartAsync()
        {
            var response = await SendWithRefreshAsync(c => c.GetAsync("api/cart"));
            if (!response.IsSuccessStatusCode) return new List<CartItemResponseDto>();
            return await response.Content.ReadFromJsonAsync<List<CartItemResponseDto>>() ?? new List<CartItemResponseDto>();
        }

        // ✅ Add to cart
        public async Task<CartItemResponseDto?> AddToCartAsync(CartItemDto dto)
        {
            var response = await SendWithRefreshAsync(c => c.PostAsJsonAsync("api/cart/add", dto));
            if (!response.IsSuccessStatusCode) return null;

            var item = await response.Content.ReadFromJsonAsync<CartItemResponseDto>();
            OnCartChanged?.Invoke();
            return item;
        }

        // ✅ Update cart item
        public async Task<CartItemResponseDto?> UpdateCartItemAsync(int cartItemId, int quantity)
        {
            var response = await SendWithRefreshAsync(c =>
                c.PutAsJsonAsync($"api/cart/update/{cartItemId}", new UpdateQuantityDto { Quantity = quantity })
            );

            if (!response.IsSuccessStatusCode) return null;

            var item = await response.Content.ReadFromJsonAsync<CartItemResponseDto>();
            OnCartChanged?.Invoke();
            return item;
        }

        // ✅ Remove cart item
        public async Task RemoveCartItemAsync(int cartItemId)
        {
            var response = await SendWithRefreshAsync(c => c.DeleteAsync($"api/cart/remove/{cartItemId}"));
            if (response.IsSuccessStatusCode) OnCartChanged?.Invoke();
        }

        // ✅ Calculate total
        public async Task<decimal> CalculateTotalAsync(List<int> selectedItemIds)
        {
            var response = await SendWithRefreshAsync(c => c.PostAsJsonAsync("api/cart/calculate-total", selectedItemIds));
            if (!response.IsSuccessStatusCode) return 0;
            return await response.Content.ReadFromJsonAsync<decimal>();
        }

        // ✅ Get cart count
        public async Task<int> GetCartCountAsync()
        {
            var items = await GetCartAsync();
            return items.Sum(ci => ci.Quantity);
        }

        // ✅ Toggle selection
        public async Task<ToggleSelectionDto?> ToggleSelectionAsync(int cartItemId, bool isSelected)
        {
            var response = await SendWithRefreshAsync(c => c.PutAsJsonAsync($"api/cart/toggle/{cartItemId}", isSelected));
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ToggleSelectionDto>();
        }
    }
}
