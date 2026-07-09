using Microsoft.AspNetCore.Mvc;       
using Microsoft.EntityFrameworkCore;           
using ShopEase.Shared.Models;         
using Microsoft.AspNetCore.Authorization;
using ShopEase.Server.Data;   
using ShopEase.Shared.DTOs;


namespace ShopEase.Server.Controllers

{
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ShopDbContext _context;

    public CartController(ShopDbContext context)
    {
        _context = context;
    }

    // ✅ GET: return DTOs, not EF entities
    // Get all cart items for a user

[Authorize]
[HttpGet]
public async Task<IActionResult> GetCart()
{
   
        var userId = GetUserIdFromClaims();
        var items = await _context.CartItems
        .Where(ci => ci.UserId == userId)
        .Include(ci => ci.Product)
        .Select(ci => new CartItemResponseDto
        {
            CartItemId = ci.CartItemId,
            ProductId = ci.ProductId,
            ProductName = ci.Product != null ? ci.Product.Name : string.Empty,
            Price = ci.Price,
            Quantity = ci.Quantity,
            TotalPrice = ci.TotalPrice,
            IsSelected = ci.IsSelected
        })
        .ToListAsync();

    return Ok(items);
}

// ✅ POST: add product to cart using CartItemDto
// Add product to cart

[HttpPost("add")]
public async Task<IActionResult> AddToCart([FromBody] CartItemDto dto)
{
    if (dto == null)
        return BadRequest("Invalid cart item payload.");

        var userId = GetUserIdFromClaims();
    var cartItem = new CartItem
    {
        UserId = userId,
        ProductId = dto.ProductId,
        Quantity = dto.Quantity,
        Price = dto.Price,
        TotalPrice = dto.Price * dto.Quantity,
        IsSelected = true
    };

    _context.CartItems.Add(cartItem);
    await _context.SaveChangesAsync();

    // ✅ Return DTO, not EF entity
    var product = await _context.Products.FindAsync(cartItem.ProductId);

    var responseDto = new CartItemResponseDto
    {
        CartItemId = cartItem.CartItemId,
        ProductId = cartItem.ProductId,
        ProductName = product?.Name ?? string.Empty,
        Price = cartItem.Price,
        Quantity = cartItem.Quantity,
        TotalPrice = cartItem.TotalPrice,
        IsSelected = cartItem.IsSelected
    };

    return Ok(responseDto);
}

    // (Optional) PUT: update quantity
    // Update quantity
    
   [HttpPut("update/{cartItemId}")]
public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateQuantityDto dto)
{
           var userId = GetUserIdFromClaims();

    var item = await _context.CartItems
        .Include(ci => ci.Product)
        .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

    if (item == null) return NotFound();

    if (item.UserId != userId) return Forbid();


    item.Quantity = dto.Quantity;
    item.TotalPrice = item.Price * dto.Quantity;
    await _context.SaveChangesAsync();

    var responseDto = new CartItemResponseDto
    {
        CartItemId = item.CartItemId,
        ProductId = item.ProductId,
        ProductName = item.Product?.Name ?? string.Empty,
        Price = item.Price,
        Quantity = item.Quantity,
        TotalPrice = item.TotalPrice,
        IsSelected = item.IsSelected
    };

    return Ok(responseDto);
}


    // Remove item
    
    [HttpDelete("remove/{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(int cartItemId)
    {
        // 1. Get the current userId from JWT claims
            var userId = GetUserIdFromClaims();

        // 2. Load the cart item from DB
        var item = await _context.CartItems.FindAsync(cartItemId);
        if (item == null) return NotFound();

        // 3. Verify ownership
    if (item.UserId != userId) return Forbid();

    // 4. Remove and save
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Calculate total price for selected items
    
[HttpPost("calculate-total")]
    public async Task<IActionResult> CalculateTotal([FromBody] List<int> selectedItemIds)
    {
          var userId = GetUserIdFromClaims();
    
            var items = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.UserId == userId 
                        && selectedItemIds.Contains(ci.CartItemId)
                        && ci.IsSelected)   // ✅ enforce DB state
            .ToListAsync();

        var total = items.Sum(ci => ci.Price * ci.Quantity);
            return Ok(total);
    }

//to check/uncheck items in the cart and persist the selection state in the database

[HttpPut("toggle/{cartItemId}")]
public async Task<IActionResult> ToggleSelection(int cartItemId, [FromBody] bool isSelected)
{
    var item = await _context.CartItems
        .Include(ci => ci.Product)
        .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

    if (item == null) return NotFound();

    item.IsSelected = isSelected;
    await _context.SaveChangesAsync();

    var userId = GetUserIdFromClaims();
    var itemTotal = item.Price * item.Quantity;
    var cartTotal = await _context.CartItems
        .Where(ci => ci.UserId == item.UserId && ci.IsSelected)
        .SumAsync(ci => ci.Price * ci.Quantity);

    var dto = new ToggleSelectionDto
    {
        CartItemId = item.CartItemId,
        IsSelected = item.IsSelected,
        ItemTotal = itemTotal,
        CartTotal = cartTotal
    };

    return Ok(dto);
}

private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
    throw new UnauthorizedAccessException("UserId claim missing");
    return int.Parse(userIdClaim);
}
}
}