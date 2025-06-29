using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "配送地址為必填")]
        [MaxLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ShippingCity { get; set; }

        [MaxLength(20)]
        public string? ShippingPostalCode { get; set; }
    }
}