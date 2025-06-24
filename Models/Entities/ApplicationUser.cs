using microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace ECommerceWebsite.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Maxlength(100)]
        public string FirstName { get; set; } = string.Empty;
        [Maxlength(100)]
        public string LastName { get; set; } = string.Empty;
        [Maxlength(200)]
        public string Address { get; set; }
        [Maxlength(100)]
        public string? City { get; set; }
        [Maxlength(20)]
        public string? PostalCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // 導航屬性
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
    }
}