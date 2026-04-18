using ASM.Data;
using ASM.Models;
using ASM.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims; // For GetUserId

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

            // BỔ SUNG 2: CHỈ lấy 5 món Best Seller dựa vào Số lượng đánh giá và Điểm đánh giá
            ViewBag.BestSellers = await _context.Products
                .Include(p => p.Reviews) 
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.Reviews.Count) // Đã gọi là Best Seller thì số lượt mua/đánh giá phải đứng đầu
                .ThenByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0) // Cùng lượt thì ưu tiên điểm cao
                .Take(5) 
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
            // CHUẨN HÓA: Sử dụng Utilities để tìm kiếm thông minh (không dấu, không khoảng trắng)
            var queryNormalized = Utilities.ToUnaccent(query);

            var allActiveProducts = await _context.Products
                .Where(p => p.Status == "Active")
                .ToListAsync();

            var results = allActiveProducts
                .Where(p => Utilities.ToUnaccent(p.ProductName).Contains(queryNormalized))
                .Select(p => new {
                    id = p.ProductId,
                    name = p.ProductName,
                    price = p.Price,
                    image = p.Image
                })
                .Take(20) // Chỉ lấy tối đa 20 kết quả để tránh bị đơ giao diện
                .ToList();

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
                .Take(6)
                .ToListAsync();

            if (relatedProducts.Any() && relatedProducts.Count < 6)
            {
                var originalCount = relatedProducts.Count;
                int j = 0;
                while (relatedProducts.Count < 6)
                {
                    relatedProducts.Add(relatedProducts[j % originalCount]);
                    j++;
                }
            }

            // KIỂM TRA QUYỀN VIẾT ĐÁNH GIÁ: Chỉ những người đã mua món này và đơn hàng đã Hoàn thành (Status = 4)
            bool canReview = false;
            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    canReview = await _context.OrderDetails
                        .AnyAsync(od => od.ProductId == id && 
                                        od.Order.UserId == userId && 
                                        od.Order.OrderStatus == 4);
                }
            }
            ViewBag.CanReview = canReview;

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // =========================================================
        // 4. GỬI ĐÁNH GIÁ (SUBMIT REVIEW)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Không xác định được danh tính người dùng." });
            }

            // Bảo mật: Kiểm tra lại xem họ đã thực sự mua món này chưa
            bool hasBought = await _context.OrderDetails
                .AnyAsync(od => od.ProductId == productId && 
                                od.Order.UserId == userId && 
                                od.Order.OrderStatus == 4);
            
            if (!hasBought)
            {
                return Json(new { success = false, message = "Bạn chỉ được đánh giá những món đã mua và đơn hàng đã hoàn thành thành công." });
            }

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

        // =========================================================
        // 6. KIỂM TRA ĐIỀU KIỆN NHẬN VOUCHER (AJAX)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> CheckPromoAjax()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null) 
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để xem quà tặng bí mật này!" });
            }

            int userId = int.Parse(claim);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null) return Json(new { success = false, message = "Lỗi xác thực người dùng." });

            // 1. Check tài khoản mới (Ví dụ: Định nghĩa tài khoản lập trong vòng 7 ngày là mới)
            TimeSpan age = DateTime.Now - user.CreatedAt;
            if (age.TotalDays > 7)
            {
                return Json(new { success = false, message = "Rất tiếc! Món quà này chỉ dành cho tài khoản mới đăng ký trong vòng 7 ngày." });
            }

            // Lấy id của Voucher NEWACCOUNT26 (hoặc tạo 1 hàm tìm ID dựa trên Mã code)
            var promoVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == "NEWACCOUNT26");
            if (promoVoucher == null)
            {
                return Json(new { success = false, message = "Chương trình khuyến mãi tạm thời chưa kích hoạt. Vui lòng thử lại sau!" });
            }

            // 2. Check xem User đã từng dùng Voucher chưa (tìm trong Order)
            bool hasUsed = await _context.Orders.AnyAsync(o => o.UserId == userId && o.VoucherId == promoVoucher.VoucherId);
            if (hasUsed)
            {
                return Json(new { success = false, message = "Khà khà, bạn đã đổi món quà này rồi mà! Cùng khám phá các mã khác nhé." });
            }

            // Trả về thời gian đếm lùi tĩnh 24h
            return Json(new { success = true, voucherCode = promoVoucher.Code });
        }

        // =========================================================
        // 7. TRUNG TÂM VOUCHER
        // =========================================================
        public async Task<IActionResult> Voucher()
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.Status == "Active" && v.EndDate >= DateTime.Now)
                .OrderByDescending(v => v.DiscountValue)
                .ToListAsync();

            // Nếu user đã đăng nhập, lấy danh sách ID voucher họ đã lưu để hiện trạng thái "Đã lưu"
            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    ViewBag.SavedVoucherIds = await _context.UserVouchers
                        .Where(uv => uv.UserId == userId)
                        .Select(uv => uv.VoucherId)
                        .ToListAsync();
                }
            }

            return View(vouchers);
        }

        [HttpGet]
        public async Task<IActionResult> SaveVoucherToDb(int voucherId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để lưu voucher!" });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Json(new { success = false });

            // Kiểm tra xem đã lưu chưa
            var exists = await _context.UserVouchers.AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
            if (exists)
            {
                 return Json(new { success = true, message = "Voucher đã có trong kho của bạn." });
            }

            var uv = new UserVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                SavedAt = DateTime.Now
            };

            _context.UserVouchers.Add(uv);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm voucher vào kho của bạn!" });
        }

        public async Task<IActionResult> MyVoucher()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Index");

            var vouchers = await _context.UserVouchers
                .Where(uv => uv.UserId == userId)
                .Include(uv => uv.Voucher)
                .OrderByDescending(uv => uv.SavedAt)
                .Select(uv => uv.Voucher)
                .ToListAsync();

            return View(vouchers);
        }

        // =========================================================
        // 8. TRANG KẾT QUẢ TÌM KIẾM (SEARCH PAGE)
        // =========================================================
        public async Task<IActionResult> Search(string query)
        {
            ViewBag.Query = query;
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new List<Product>());
            }

            // Chuẩn hóa từ khóa tìm kiếm
            var queryNormalized = Utilities.ToUnaccent(query);

            // Lấy toàn bộ sản phẩm đang hoạt động
            var allActiveProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .ToListAsync();

            // Lọc thông minh trong bộ nhớ
            var results = allActiveProducts
                .Where(p => Utilities.ToUnaccent(p.ProductName).Contains(queryNormalized))
                .ToList();

            return View(results);
        }

        // API: Kiểm tra điều kiện hiển thị Gift Box cho tài khoản mới
        [HttpGet]
        public async Task<IActionResult> GetNewAccountGiftBox()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { show = false });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { show = false });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { show = false });

            // Chỉ hiện trong vòng 7 ngày kể từ khi tạo
            var daysSinceCreation = (DateTime.Now - user.CreatedAt).TotalDays;
            if (daysSinceCreation > 7) return Json(new { show = false });

            // Tính thời gian còn lại (voucher tồn tại 48h từ lúc tạo)
            var voucherExpiry = user.CreatedAt.AddHours(48);
            var timeLeft = voucherExpiry - DateTime.Now;
            if (timeLeft.TotalSeconds <= 0) return Json(new { show = false, expired = true });

            // Kiểm tra user đã lưu voucher này chưa
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == "NEWACCOUNT26");
            if (voucher == null) return Json(new { show = false });

            // AUTO-FIX: Nếu voucher đang bị sai giá trị (ví dụ 50%) thì ép lên 90% cho đúng thiết kế
            if (voucher.DiscountValue != 90)
            {
                voucher.DiscountValue = 90;
                voucher.Name = "Ưu đãi người mới - Giảm 90%";
                await _context.SaveChangesAsync();
            }

            var alreadySaved = await _context.UserVouchers
                .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.VoucherId);

            return Json(new
            {
                show = true,
                alreadySaved = alreadySaved,
                voucherCode = voucher.Code,
                voucherName = voucher.Name,
                discountText = "90%",
                expiryTimestamp = ((DateTimeOffset)voucherExpiry).ToUnixTimeSeconds(),
                voucherId = voucher.VoucherId
            });
        }
    }
}