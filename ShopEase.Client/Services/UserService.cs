using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ShopEase.Shared.Models;
using Blazored.LocalStorage;
using System.ComponentModel.DataAnnotations; // ✅ needed for validation
using System; // for Console logging
using ShopEase.Shared.DTOs;


namespace ShopEase.Client.Services
{
    public class UserService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;

        public UserService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        // 🔹 Register new user
        public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        // ✅ Client-side validation before sending
        var validationContext = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, results, true))
        {
            Console.WriteLine("Register validation failed: " +
                string.Join(", ", results.Select(r => r.ErrorMessage)));
            return false;
        }

        var response = await _http.PostAsJsonAsync("api/user/register", request);
        return response.IsSuccessStatusCode;
    }

        // 🔹 Login and store JWT + refresh token
        public async Task<string?> Login(string email, string password)
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("api/user/login", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse == null) return null;

            await _localStorage.SetItemAsync("jwt", loginResponse.JwtToken);
            await _localStorage.SetItemAsync("refreshToken", loginResponse.RefreshToken);

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", loginResponse.JwtToken);

            return loginResponse.JwtToken; // ✅ still return string for compatibility
        }

        // 🔹 Refresh JWT using stored refresh token
        public async Task<bool> RefreshJwtAsync()
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var refreshResponse = await _http.PostAsJsonAsync("api/user/refresh",
                new RefreshRequest { RefreshToken = refreshToken });

            if (!refreshResponse.IsSuccessStatusCode) return false;

            var newJwt = await refreshResponse.Content.ReadFromJsonAsync<JwtResponse>();
            if (newJwt == null) return false;

            await _localStorage.SetItemAsync("jwt", newJwt.JwtToken);

            Console.WriteLine("Refreshing JWT... New token issued at " + DateTime.Now);

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", newJwt.JwtToken);

            return true;
        }

        // 🔹 Get all users (Admin only)
        public async Task<List<User>?> GetUsers()
        {
            var response = await _http.GetAsync("api/user");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await RefreshJwtAsync())
                {
                    response = await _http.GetAsync("api/user"); // retry
                }
            }
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<User>>()
                : null;
        }

        // 🔹 Get single user by ID
        public async Task<User?> GetUser(int id)
        {
            var response = await _http.GetAsync($"api/user/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await RefreshJwtAsync())
                {
                    response = await _http.GetAsync($"api/user/{id}");
                }
            }
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<User>()
                : null;
        }

        // 🔹 Update user
        public async Task<bool> UpdateUser(User user)
        {
            // ✅ Client-side validation before sending
            var validationContext = new ValidationContext(user);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(user, validationContext, results, true))
            {
                Console.WriteLine("UpdateUser validation failed: " +
                    string.Join(", ", results.Select(r => r.ErrorMessage)));
                return false;
            }

            var response = await _http.PutAsJsonAsync($"api/user/{user.UserId}", user);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await RefreshJwtAsync())
                {
                    response = await _http.PutAsJsonAsync($"api/user/{user.UserId}", user);
                }
            }
            return response.IsSuccessStatusCode;
        }

        // 🔹 Delete user
        public async Task<bool> DeleteUser(int id)
        {
            var response = await _http.DeleteAsync($"api/user/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await RefreshJwtAsync())
                {
                    response = await _http.DeleteAsync($"api/user/{id}");
                }
            }
            return response.IsSuccessStatusCode;
        }

        // 🔹 Promote user to Admin (Admin only)
        public async Task<bool> PromoteToAdmin(string email)
        {
            var request = new { Email = email };
            var response = await _http.PostAsJsonAsync("api/user/promote", request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await RefreshJwtAsync())
                {
                    response = await _http.PostAsJsonAsync("api/user/promote", request);
                }
            }
            return response.IsSuccessStatusCode;
        }
    }
}
