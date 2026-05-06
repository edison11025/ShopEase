using Microsoft.EntityFrameworkCore;
using ShopEase.Shared.Models; // User, Product, CartItem, RefreshToken models live here

namespace ShopEase.Server.Data
{
    public class ShopDbContext : DbContext
    {
        public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User ↔ CartItem (1-to-many)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product ↔ CartItem (1-to-many)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // User ↔ RefreshToken (1-to-many)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision for prices
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.TotalPrice)
                .HasColumnType("decimal(18,2)");

            // ✅ Seed Admin user
            modelBuilder.Entity<User>().HasData(
                new User {
                    UserId = 1,
                    UserName = "AdminUser",
                    Email = "admin@shopease.com",
                    PasswordHash = "AQAAAAEAACcQAAAAED6kjN/loagAsJy7TTW91LmYZawGgU0H+4mmtpCaYRa5lZGmPMBq5LJV6VhFSLxAjg==",
                    Role = "Admin"
                }
            );

            // ✅ Seed demo products (compressed images)
            modelBuilder.Entity<Product>().HasData(
                new Product {
                    ProductId = 1,
                    Name = "Sample Pencil",
                    Price = 1.50m,
                    Stock = 100,
                    Category = "Stationery",
                    ImageUrl = "images/pencil.png",   // compressed 400x400 version
                    Description = "A simple seeded product for demo."
                },
                new Product {
                    ProductId = 2,
                    Name = "Coffee Mug",
                    Price = 5.99m,
                    Stock = 30,
                    Category = "Kitchenware",
                    ImageUrl = "images/mug.png",      // compressed 400x400 version
                    Description = "A ceramic mug for hot drinks."
                }
            );
        }
    }
}
