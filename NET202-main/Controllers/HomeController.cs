using ASM.Data;
using ASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ASM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 1. TRANG CHỦ / THỰC ĐƠN
        // =========================================================
        public async Task<IActionResult> Index(int? categoryId)
        {
            // BỔ SUNG 1: Lấy danh sách Danh mục đang hoạt động để làm menu chọn
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == "Active")
                .ToListAsync();

            // BỔ SUNG 2: CHỈ lấy 3 món Best Seller ở trạng thái Đang bán (Active)
            ViewBag.BestSellers = await _context.Products
                .Where(p => p.Status == "Active")
                .Take(3)
                .ToListAsync();

            // BỔ SUNG 3: Lọc danh sách món ăn, ép buộc chỉ hiện món Đang bán
            var productQuery = _context.Products
                .Include(p => p.Category) // Kéo theo Category để lỡ View cần dùng tên danh mục
                .Where(p => p.Status == "Active")
                .AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
            {
                productQuery = productQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await productQuery.ToListAsync();

            return View(products);
        }

        // =========================================================
        // 2. TÌM KIẾM TRỰC TIẾP (LIVE SEARCH AJAX)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> SearchAjax(string query)
        {
            // BỔ SUNG 4: Thanh tìm kiếm cũng CHỈ được phép tìm ra món Đang bán
            var productQuery = _context.Products
                .Where(p => p.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productQuery = productQuery.Where(p => p.ProductName.Contains(query));
            }

            var results = await productQuery
                .Select(p => new {
                    id = p.ProductId,
                    name = p.ProductName,
                    price = p.Price,
                    image = p.Image
                })
                .Take(20) // Chỉ lấy tối đa 20 kết quả để tránh bị đơ giao diện
                .ToListAsync();

            return Json(results);
        }

        // =========================================================
        // 3. XEM CHI TIẾT SẢN PHẨM
        // =========================================================
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            
            if (product == null || product.Status == "Hidden") 
            {
                return NotFound();
            }

            // Mock reviews if none exist (for demo purposes as requested)
            if (product.Reviews == null || !product.Reviews.Any())
            {
                product.Reviews = new List<Review>
                {
                    new Review { Rating = 5, Comment = "Vị cafe rất đậm đà, bánh croissant cực kỳ giòn và thơm bơ. Chắc chắn sẽ quay lại!", CreatedAt = DateTime.Now.AddDays(-2), User = new User { FullName = "Nguyễn Văn Anh" } },
                    new Review { Rating = 4, Comment = "Đồ uống ngon, decor quán rất đẹp (tôi đã mua mang về). Giao hàng nhanh, đóng gói cẩn thận.", CreatedAt = DateTime.Now.AddDays(-5), User = new User { FullName = "Trần Thị Bình" } },
                    new Review { Rating = 5, Comment = "Đây là món tủ của mình! Lần nào tới Bread & Brew cũng phải gọi món này.", CreatedAt = DateTime.Now.AddDays(-10), User = new User { FullName = "Lê Minh Tâm" } }
                };
            }

            // Lấy danh sách sản phẩm liên quan (cùng danh mục, trừ sản phẩm hiện tại)
            var relatedProducts = await _context.Products
                .Where(p => p.ProductId != id && p.Status == "Active")
                .OrderByDescending(p => p.CategoryId == product.CategoryId)
                .Take(4)
                .ToListAsync();

            if (relatedProducts.Count > 0 && relatedProducts.Count < 4)
            {
                var originalCount = relatedProducts.Count;
                int j = 0;
                while (relatedProducts.Count < 4)
                {
                    relatedProducts.Add(relatedProducts[j % originalCount]);
                    j++;
                }
            }

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // =========================================================
        // 4. GỬI ĐÁNH GIÁ (SUBMIT REVIEW)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            // Kiểm tra đăng nhập (Demo: Nếu chưa có Session User thì lấy User đầu tiên hoặc báo lỗi)
            // Ở đây tôi giả định bạn dùng Session["UserId"] hoặc ASP.NET Identity
            // Để demo chạy được, tôi sẽ lấy User ID mặc định là 1 nếu không tìm thấy
            int userId = 1; 

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now,
                Status = 1
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá sản phẩm!" });
        }
        // =========================================================
        // 5. TRANG VỀ CHÚNG TÔI (ABOUT US)
        // =========================================================
        public IActionResult AboutUs()
        {
            return View();
        }
    }
}