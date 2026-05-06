namespace ShopEase.Shared.DTOs
{
    public class ToggleSelectionDto
    {
        public int CartItemId { get; set; }
        public bool IsSelected { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal CartTotal { get; set; }
    }
}
