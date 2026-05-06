namespace ShopEase.Shared.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }

        // Foreign key to User
        public int UserId { get; set; }
        public User User { get; set; }   // navigation property
    }
}
