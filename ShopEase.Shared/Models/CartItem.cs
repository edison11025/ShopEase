using System.ComponentModel.DataAnnotations;

namespace ShopEase.Shared.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        // Foreign keys
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Attributes
        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }   // snapshot of product price

        [Required, Range(0.01, double.MaxValue)]
        public decimal TotalPrice { get; set; }

 // ✅ New property for persistence
        public bool IsSelected { get; set; } = true; // default: selected        

        // Navigation properties
       public User? User { get; set; }
public Product? Product { get; set; }

    }
}
