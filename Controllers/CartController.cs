using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models.Entities;
using ECommerceWebsite.Models.ViewModels;

namespace ECommerceWebsite.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.ShoppingCartItems
                .Include(s => s.Product)
                .ThenInclude(p => p.Category)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var viewModel = new CartViewModel
            {
                CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductPrice = item.Product.DiscountedPrice ?? item.Product.Price,
                    Quantity = item.Quantity,
                    ImageUrl = item.Product.ImageUrl,
                    StockQuantity = item.Product.StockQuantity
                }).ToList()
            };

            viewModel.TotalAmount = viewModel.CartItems.Sum(item => item.ProductPrice * item.Quantity);

            return View(viewModel);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "請先登入" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "商品不存在" });
            }

            if (product.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "庫存不足" });
            }

            var existingCartItem = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == productId);

            if (existingCartItem != null)
            {
                if (product.StockQuantity < existingCartItem.Quantity + quantity)
                {
                    return Json(new { success = false, message = "庫存不足" });
                }
                existingCartItem.Quantity += quantity;
                _context.Update(existingCartItem);
            }
            else
            {
                var cartItem = new ShoppingCartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            
            var cartCount = await _context.ShoppingCartItems
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Quantity);

            return Json(new { success = true, message = "已加入購物車", cartCount });
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.ShoppingCartItems
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == cartItemId && s.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "購物車項目不存在" });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "數量必須大於0" });
            }

            if (cartItem.Product.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "庫存不足" });
            }

            cartItem.Quantity = quantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            var itemTotal = (cartItem.Product.DiscountedPrice ?? cartItem.Product.Price) * quantity;
            
            return Json(new { success = true, itemTotal = itemTotal.ToString("C") });
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(s => s.Id == cartItemId && s.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "購物車項目不存在" });
            }

            _context.ShoppingCartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "已從購物車移除" });
        }

        // GET: Cart/GetCartCount
        public async Task<IActionResult> GetCartCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _context.ShoppingCartItems
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Quantity);

            return Json(new { count });
        }
    }
}