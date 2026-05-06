using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ShopEase.Client.Services
{
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    // This method is to get jwt.claims and jwtAuth from localstorage
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("jwt");       //getting from localstorage

        ClaimsIdentity identity;

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                identity = new ClaimsIdentity(
                    jwt.Claims, 
                    authenticationType: "jwtAuth",
                    nameType: ClaimTypes.Name,
                    roleType: ClaimTypes.Role);  //jwt.Claims contains : email, UserId and role
            }                                                           //jwtAuth contains
            catch
            {
                // If token is invalid, fallback to anonymous
                identity = new ClaimsIdentity();
            }
        }
        else
        {
            identity = new ClaimsIdentity(); // anonymous
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);      // sent into GetAuthenticationStateAsync() in ProductCard.razor
    }

    public void NotifyUserAuthentication(string token)  // called from Login.razor after successful login, receiving the token (JwtToken) from Login.razor and extracting claims from it to create ClaimsPrincipal and notify authentication state change to the app.
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var identity = new ClaimsIdentity(jwt.Claims, "jwtAuth");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogoutAsync()
{
    // Clear JWT from local storage
    await _localStorage.RemoveItemAsync("jwt");

    // Notify Blazor that the user is now anonymous
    NotifyUserLogout();
}

}
}