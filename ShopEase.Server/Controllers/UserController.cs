using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ShopEase.Server.Data;
using ShopEase.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ShopEase.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;


namespace ShopEase.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ShopDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _hasher;

        public UserController(ShopDbContext context, IConfiguration config, IPasswordHasher<User> hasher)
        {
            _context = context;
            _config = config;
            _hasher = hasher;
        }


    private bool IsSelfOrAdmin(int targetUserId)
{
    var userIdClaim = User.FindFirst("userId")?.Value;
    var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

    if (string.IsNullOrEmpty(userIdClaim))
        return false;

    int currentUserId = int.Parse(userIdClaim);

    // ✅ Allow if self or admin
    return currentUserId == targetUserId || roleClaim == "Admin";
}

        // -------------------------
        // CRUD Endpoints
        // -------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (!IsSelfOrAdmin(id)) return Forbid();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, user.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != user.UserId) return BadRequest();

            if (!IsSelfOrAdmin(id)) return Forbid();

            _context.Entry(user).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
                    return NotFound();
                else throw;
            }
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsSelfOrAdmin(id)) return Forbid();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // -------------------------
        // Auth Endpoints
        // -------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)   //from UserService, receiving parameter :  newUser (UserName, Email, Phone and Password) to this endpoint for registration
        {
            if (!ModelState.IsValid)            // ✅ Server-side validation
                return BadRequest(ModelState);

            var hasher = new PasswordHasher<User>();   

            var user = new User
                {
                    Email = request.Email,
                    UserName = request.UserName,
                    Role = "Customer"
                };

            user.PasswordHash = hasher.HashPassword(user, request.Password);

            // Default role assignment
            user.Role = "Customer";

            _context.Users.Add(user);
            await _context.SaveChangesAsync();  // Save new user to database
            return Ok("User registered successfully");  // Return the created user (with UserId) if registration is successful
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                Console.WriteLine($"Login failed: user not found for {request.Email}");
                return Unauthorized("Invalid credentials");
            }

            Console.WriteLine($"Login attempt: {request.Email} / {request.Password}");
            Console.WriteLine($"Stored hash: {user.PasswordHash}");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            Console.WriteLine($"Verification result: {result}");

            if (result == PasswordVerificationResult.Failed)
            {
                Console.WriteLine("Password verification failed.");
                return Unauthorized("Invalid credentials");
            }

            var role = string.IsNullOrEmpty(user.Role) ? "User" : user.Role;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim("userId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty) // ✅ Username for display
            };

            var jwtKey = _config["Jwt:Key"] ?? throw new Exception("Jwt:Key missing in appsettings.json");
            var jwtIssuer = _config["Jwt:Issuer"] ?? throw new Exception("Jwt:Issuer missing in appsettings.json");
            var jwtAudience = _config["Jwt:Audience"] ?? throw new Exception("Jwt:Audience missing in appsettings.json");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            var jwtString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            var refreshToken = Guid.NewGuid().ToString();
            var refreshExpiry = DateTime.Now.AddDays(7);

            var userToken = new RefreshToken
            {
                Token = refreshToken,
                Expires = refreshExpiry,
                UserId = user.UserId
            };
            _context.RefreshTokens.Add(userToken);
            await _context.SaveChangesAsync();  // save RefreshToken to database

            return Ok(new LoginResponse     // send both JwtToken and RefreshToken to Login.razor after successful login. Client will store them in localstorage and use them for subsequent requests and token refresh.
            {
                JwtToken = jwtString,       // this JwtToken containes claims (email, userId and role), issuer, audience, expires, signingCredentials and is signed with the key defined in appsettings.json. This token is sent to client and stored in localstorage with key "jwt".
                RefreshToken = refreshToken // RefreshToken containes Token, Expires and UserId
            }); 
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (storedToken == null || storedToken.Expires < DateTime.Now)
                return Unauthorized("Invalid or expired refresh token");

            var user = await _context.Users.FindAsync(storedToken.UserId);
            if (user == null) return Unauthorized("User not found");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("userId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is missing in configuration.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var newJwt = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            var jwtString = new JwtSecurityTokenHandler().WriteToken(newJwt);

            return Ok(new JwtResponse
            {
                JwtToken = jwtString
            });
        }

        // -------------------------
        // Role Management Endpoint
        // -------------------------

        [Authorize(Roles = "Admin")]
        [HttpPut("promote/{id}")]
        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = "Admin";
            await _context.SaveChangesAsync();
            return Ok(user);
        }
    }

    

}
