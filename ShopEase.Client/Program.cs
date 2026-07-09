using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShopEase.Client;
using ShopEase.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🔹 Register HttpClient
builder.Services.AddScoped(sp =>
{
    var baseAddress = builder.HostEnvironment.BaseAddress;

    if (!baseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        baseAddress = baseAddress.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
    }

    return new HttpClient
    {
        BaseAddress = new Uri(baseAddress)
    };
});

// 🔹 Register LocalStorage
builder.Services.AddBlazoredLocalStorage();

// 🔹 Register your services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();
