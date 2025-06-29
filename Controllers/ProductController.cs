using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models.Entities;
using X.PagedList;
using X.PagedList;

namespace ECommerceWebsite.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index(int? page, int? categoryId, string? search)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search) ||
                                             p.Description!.Contains(search));
            }

            products = products.OrderBy(p => p.Name);

            var pageNumber = page ?? 1;
            var pageSize = 12;
            var pagedProducts = await products.ToPagedListAsync(pageNumber, pageSize);

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            return View(pagedProducts);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }


}