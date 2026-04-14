using ASM.Data;
using ASM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Thêm thư viện này để lấy ID người dùng

namespace ASM.Controllers
{
    // ==============================================================
    // 1. CLASS XỬ LÝ GIỎ HÀNG VÀ THANH TOÁN
    // ==============================================================
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CartController(ApplicationDbContext context) { _context = context; }

        // Hàm phụ: Lấy ID của người dùng đang đăng nhập từ Cookie
        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0;
        }

        // 1. LẤY GIỎ HÀNG (Đã cập nhật tính tiền Voucher)
        [HttpGet]
        public IActionResult GetCart()
        {
            int uId = GetUserId();
            if (uId == 0) return Json(new { items = new List<object>(), total = 0, discount = 0, finalTotal = 0 });

            var cartItems = _context.Carts.Include(x => x.Product).Where(x => x.UserId == uId).ToList();
            decimal total = cartItems.Sum(x => x.Product.Price * x.Quantity);
            decimal discount = 0;

            // Kiểm tra xem người dùng có áp dụng mã giảm giá trên giỏ hàng chưa
            string voucherCode = HttpContext.Session.GetString("VoucherCode");
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == voucherCode && v.Status == "Active");
                if (voucher != null && total >= voucher.MinOrderValue)
                {
                    if (voucher.DiscountType == 1) discount = (total * voucher.DiscountValue) / 100;
                    else discount = voucher.DiscountValue;

                    if (discount > total) discount = total; // Không giảm quá tổng tiền
                }
            }

            var items = cartItems.Select(x => new {
                productId = x.ProductId,
                productName = x.Product.ProductName,
                price = x.Product.Price,
                image = x.Product.Image,
                quantity = x.Quantity
            }).ToList();

            // Trả về Object để JS có thể đọc được tổng tiền và tiền giảm
            return Json(new { items = items, total = total, discount = discount, finalTotal = total - discount });
        }

        // 2. THÊM VÀO GIỎ
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            int uId = GetUserId();
            if (uId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                _context.Carts.Add(new Cart { ProductId = id, UserId = uId, Quantity = 1 });
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // 2b. MUA NGAY (Gộp Thêm giỏ + Redirect đến trang đơn hàng)
        public async Task<IActionResult> BuyNow(int productId)
        {
            int uId = GetUserId();
            if (uId == 0) return RedirectToAction("Login", "Account");

            var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == uId);
            if (cartItem == null)
            {
                _context.Carts.Add(new Cart { ProductId = productId, UserId = uId, Quantity = 1 });
                await _context.SaveChangesAsync();
            }

            // Gọi hàm Checkout nội bộ để tạo đơn hàng nháp và chuyển hướng sang trang chi tiết đơn hàng đó
            var result = (dynamic)((JsonResult)await Checkout()).Value;
            if (result.success == true)
            {
                return RedirectToAction("Detail", "Order", new { id = result.orderId, isConfirm = true });
            }

            return RedirectToAction("Index", "Home");
        }

        // 3. TĂNG SỐ LƯỢNG
        [HttpPost]
        public IActionResult Increase(int id)
        {
            int uId = GetUserId();
            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null) { 
                item.Quantity++; 
                _context.SaveChanges(); 
            }
            return Json(new { success = true });
        }

        // 4. GIẢM SỐ LƯỢNG
        [HttpPost]
        public IActionResult Decrease(int id)
        {
            int uId = GetUserId();
            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0) _context.Carts.Remove(item);
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }

        // 5. XOÁ SẢN PHẨM KHỎI GIỎ
        [HttpPost]
        public IActionResult Remove(int id)
        {
            int uId = GetUserId();
            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null) { 
                _context.Carts.Remove(item); 
                _context.SaveChanges(); 
            }
            return Json(new { success = true });
        }

        // 6. ÁP DỤNG VOUCHER (Lưu vào Session)
        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == code && v.Status == "Active");
            if (voucher == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
            
            if (DateTime.Now < voucher.StartDate || DateTime.Now > voucher.EndDate)
                return Json(new { success = false, message = "Voucher không trong thời gian sử dụng!" });
                
            if (voucher.UsedCount >= voucher.UsageLimit)
                return Json(new { success = false, message = "Voucher đã hết lượt sử dụng!" });

            // Lưu mã hợp lệ vào Session
            HttpContext.Session.SetString("VoucherCode", code);
            return Json(new { success = true });
        }

        // 7. THANH TOÁN (TỪ GIỎ HÀNG SANG ĐƠN HÀNG - Đã gộp tính Voucher)
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            int uId = GetUserId();
            var cartItems = _context.Carts
                .Include(x => x.Product)
                .Where(x => x.UserId == uId)
                .ToList();

            if (cartItems == null || !cartItems.Any())
                return Json(new { success = false, message = "Giỏ hàng rỗng" });

            decimal totalAmount = cartItems.Sum(x => x.Product.Price * x.Quantity);
            decimal discountAmount = 0;
            int? voucherId = null;

            // Đọc lại Voucher từ Session để áp dụng
            string voucherCode = HttpContext.Session.GetString("VoucherCode");
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == voucherCode && v.Status == "Active");
                if (voucher != null && totalAmount >= voucher.MinOrderValue)
                {
                    if (voucher.DiscountType == 1) discountAmount = (totalAmount * voucher.DiscountValue) / 100;
                    else discountAmount = voucher.DiscountValue;
                    if (discountAmount > totalAmount) discountAmount = totalAmount;
                    
                    voucherId = voucher.VoucherId;
                    voucher.UsedCount++; // Cộng 1 lượt sử dụng
                }
            }

            var order = new Order
            {
                UserId = uId,
                CreatedAt = DateTime.Now,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                FinalAmount = totalAmount - discountAmount,
                PaymentMethod = 1, 
                PaymentStatus = 0, 
                OrderStatus = 1, // Đang chờ xác nhận
                VoucherId = voucherId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); 

            foreach (var item in cartItems)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                };
                _context.OrderDetails.Add(detail);
            }

            _context.SaveChanges();

            // Xoá sạch giỏ hàng và Xóa Session Voucher sau khi đã lên đơn
            _context.Carts.RemoveRange(cartItems);
            HttpContext.Session.Remove("VoucherCode");
            _context.SaveChanges();

            return Json(new { success = true, orderId = order.OrderId });
        }
    }

    // ==============================================================
    // 2. CLASS QUẢN LÝ LỊCH SỬ ĐƠN HÀNG
    // ==============================================================
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context) { _context = context; }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0;
        }

        // TRANG DANH SÁCH ĐƠN HÀNG
        public IActionResult Index()
        {
            int userId = GetUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        // TRANG CHI TIẾT ĐƠN HÀNG
        public IActionResult Detail(int id, bool isConfirm = false)
        {
            int userId = GetUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound("Không tìm thấy đơn hàng");

            ViewBag.IsConfirm = isConfirm;
            return View(order);
        }

        // XÁC NHẬN THÔNG TIN GIAO HÀNG (Cập nhật SĐT, Địa chỉ từ View Order/Detail)
        
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(int OrderId, string PhoneNumber, string ShippingAddress, int PaymentMethod, string? Note) 
        {
            var order = await _context.Orders.FindAsync(OrderId);
            if (order != null)
            {
                order.OrderStatus = 1; // 1: Chờ xử lý
                order.PaymentMethod = PaymentMethod;
                
                // DÒNG NÀY LÀ QUAN TRỌNG NHẤT ĐỂ LƯU GHI CHÚ
                order.Note = Note; 
                
                // Lấy thông tin mặc định từ User nếu form không gửi (theo layout của bạn)
                var user = await _context.Users.FindAsync(order.UserId);
                order.PhoneNumber = string.IsNullOrEmpty(PhoneNumber) ? user?.Phone : PhoneNumber;
                order.ShippingAddress = string.IsNullOrEmpty(ShippingAddress) ? user?.Address : ShippingAddress;

                await _context.SaveChangesAsync();
            }
            
            if (order != null && order.PaymentMethod == 2)
            {
                return RedirectToAction("PaymentQR", new { id = order.OrderId });
            }
            
            return RedirectToAction("Index"); 
        }

        // TRANG THANH TOÁN QR
        public IActionResult PaymentQR(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // TRANG ĐẶT HÀNG THÀNH CÔNG
        public IActionResult OrderSuccess()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            int userId = GetUserId();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);
            
            if (order != null && order.OrderStatus == 1)
            {
                order.OrderStatus = 5; // 5: Đã hủy
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã hủy đơn hàng thành công" });
            }
            
            return Json(new { success = false, message = "Không thể hủy đơn hàng này" });
        }

        // ==============================================================
        // 3. XỬ LÝ GỬI ĐÁNH GIÁ (BACKEND FOR REVIEW MODAL)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewSubmission model)
        {
            int userId = GetUserId();
            if (userId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (model.Rating < 1 || model.Rating > 5)
                return Json(new { success = false, message = "Số sao không hợp lệ" });

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.UserId == userId);

            if (order == null) return Json(new { success = false, message = "Đơn hàng không tồn tại" });

            // Với mỗi sản phẩm trong đơn hàng, chúng ta tạo một bản ghi Review
            // Điều này giúp đánh giá xuất hiện ở trang chi tiết của từng sản phẩm
            foreach (var item in order.OrderDetails)
            {
                // Kiểm tra xem đã đánh giá sản phẩm này trong đơn này chưa
                var existing = await _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == model.OrderId && r.ProductId == item.ProductId);
                if (existing != null)
                {
                    existing.Rating = model.Rating;
                    existing.Comment = model.Comment;
                    existing.CreatedAt = DateTime.Now;
                }
                else
                {
                    var review = new Review
                    {
                        UserId = userId,
                        OrderId = model.OrderId,
                        ProductId = item.ProductId,
                        Rating = model.Rating,
                        Comment = model.Comment,
                        Status = 1,
                        CreatedAt = DateTime.Now
                    };
                    _context.Reviews.Add(review);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // Helper Model for SubmitReview
    public class ReviewSubmission
    {
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}