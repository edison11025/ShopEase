using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopEase.Server.Controllers;
using ShopEase.Server.Data;
using ShopEase.Shared.DTOs;
using ShopEase.Shared.Models;

namespace ShopEase.Tests;

public class UserControllerTests
{
    private static UserController CreateController(ShopDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-test-secret-key-with-32-chars",
                ["Jwt:Issuer"] = "https://localhost",
                ["Jwt:Audience"] = "https://localhost"
            })
            .Build();

        return new UserController(context, configuration, new PasswordHasher<User>());
    }

    private static ShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ShopDbContext(options);
    }

    [Fact]
    public async Task Register_ReturnsOkAndCreatesUser()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            UserName = "newuser",
            Password = "Password1!"
        };

        var result = await controller.Register(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User registered successfully", okResult.Value);

        var savedUser = await context.Users.SingleAsync();
        Assert.Equal(request.Email, savedUser.Email);
        Assert.Equal(request.UserName, savedUser.UserName);
        Assert.Equal("Customer", savedUser.Role);
        Assert.False(string.IsNullOrWhiteSpace(savedUser.PasswordHash));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtAndRefreshToken()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var password = "Password1!";
        var user = new User
        {
            Email = "loginuser@example.com",
            UserName = "loginuser",
            Role = "Customer"
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, password);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await controller.Login(new LoginRequest
        {
            Email = user.Email,
            Password = password
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);

        Assert.False(string.IsNullOrWhiteSpace(loginResponse.JwtToken));
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.RefreshToken));

        var refreshToken = await context.RefreshTokens.SingleAsync();
        Assert.Equal(user.UserId, refreshToken.UserId);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var password = "Password1!";
        var user = new User
        {
            Email = "badpassword@example.com",
            UserName = "badpassword",
            Role = "Customer"
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, password);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await controller.Login(new LoginRequest
        {
            Email = user.Email,
            Password = "WrongPassword!"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Empty(await context.RefreshTokens.ToListAsync());
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        context.Users.Add(new User
        {
            Email = "duplicate@example.com",
            UserName = "existinguser",
            PasswordHash = "hash",
            Role = "Customer"
        });
        await context.SaveChangesAsync();

        var result = await controller.Register(new RegisterRequest
        {
            Email = "duplicate@example.com",
            UserName = "newuser",
            Password = "Password1!"
        });

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Email already registered", conflictResult.Value);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Password1!"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var request = new RegisterRequest
        {
            Email = "not-an-email",
            UserName = "ab",
            Password = "weak"
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        Assert.False(isValid);

        foreach (var validationResult in validationResults)
        {
            foreach (var memberName in validationResult.MemberNames)
            {
                controller.ModelState.AddModelError(memberName, validationResult.ErrorMessage!);
            }
        }

        var result = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
