using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models.Entities;
using ECommerceWebsite.Models.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;

namespace ECommerceWebsite.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Order
        public async Task<IActionResult> Index(int? page)
        {
            var userId = _userManager.GetUserId(User);
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt);

            var pageNumber = page ?? 1;
            var pageSize = 10;
            var pagedOrders = await orders.ToPagedListAsync(pageNumber, pageSize);

            return View(pagedOrders);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var cartItems = await _context.ShoppingCartItems
                .Include(s => s.Product)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "購物車是空的";
                return RedirectToAction("Index", "Cart");
            }

            // 檢查庫存
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"商品 {item.Product.Name} 庫存不足";
                    return RedirectToAction("Index", "Cart");
                }
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductPrice = item.Product.DiscountedPrice ?? item.Product.Price,
                    Quantity = item.Quantity,
                    ImageUrl = item.Product.ImageUrl
                }).ToList(),
                ShippingAddress = user?.Address ?? string.Empty,
                ShippingCity = user?.City ?? string.Empty,
                ShippingPostalCode = user?.PostalCode ?? string.Empty
            };

            viewModel.TotalAmount = viewModel.CartItems.Sum(item => item.ProductPrice * item.Quantity);

            return View(viewModel);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.ShoppingCartItems
                .Include(s => s.Product)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "購物車是空的";
                return RedirectToAction("Index", "Cart");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 再次檢查庫存
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        TempData["Error"] = $"商品 {item.Product.Name} 庫存不足";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // 創建訂單
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = cartItems.Sum(item => (item.Product.DiscountedPrice ?? item.Product.Price) * item.Quantity),
                    Status = OrderStatus.Pending,
                    ShippingAddress = model.ShippingAddress ?? string.Empty,
                    ShippingCity = model.ShippingCity ?? string.Empty,
                    ShippingPostalCode = model.ShippingPostalCode ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 創建訂單項目並更新庫存
                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.DiscountedPrice ?? item.Product.Price,
                        TotalPrice = (item.Product.DiscountedPrice ?? item.Product.Price) * item.Quantity
                    };

                    _context.OrderItems.Add(orderItem);

                    // 更新庫存
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        _context.Update(product);
                    }
                }

                // 清空購物車
                _context.ShoppingCartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "訂單已成功建立";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "訂單建立失敗，請稍後再試";
                return View(model);
            }
        }

        // POST: Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "只能取消待處理的訂單";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 恢復庫存
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        _context.Update(item.Product);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _context.Update(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "訂單已取消";
            }
            catch (Exception)
            {
                // 記錄錯誤
                TempData["Error"] = "取消訂單時發生錯誤";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}