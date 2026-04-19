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
            string appliedCode = string.Empty;

            // Chỉ áp dụng voucher nếu người dùng đã bấm ÁP DỤNG.
            var manualVoucherApplied = HttpContext.Session.GetInt32("VoucherAppliedManual") == 1;
            var sessionVoucherCode = manualVoucherApplied ? HttpContext.Session.GetString("VoucherCode") : null;
            if (!manualVoucherApplied && !string.IsNullOrEmpty(HttpContext.Session.GetString("VoucherCode")))
            {
                HttpContext.Session.Remove("VoucherCode");
            }

            if (!string.IsNullOrEmpty(sessionVoucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code.ToUpper() == sessionVoucherCode.ToUpper() && v.Status == "Active");
                if (voucher != null && DateTime.Now >= voucher.StartDate && DateTime.Now <= voucher.EndDate && voucher.UsedCount < voucher.UsageLimit)
                {
                    if (total >= voucher.MinOrderValue)
                    {
                        discount = voucher.DiscountType == 1 ? (total * voucher.DiscountValue / 100) : voucher.DiscountValue;
                        if (discount > total) discount = total;
                        appliedCode = voucher.Code;
                    }
                    else
                    {
                        // Voucher hết điều kiện vì đơn hàng nhỏ hơn min order
                        HttpContext.Session.Remove("VoucherCode");
                        HttpContext.Session.Remove("VoucherAppliedManual");
                    }
                }
                else
                {
                    HttpContext.Session.Remove("VoucherCode");
                    HttpContext.Session.Remove("VoucherAppliedManual");
                }
            }

            var items = cartItems.Select(x => new
            {
                productId = x.ProductId,
                productName = x.Product.ProductName,
                price = x.Product.Price,
                image = x.Product.Image,
                quantity = x.Quantity
            }).ToList();

            return Json(new
            {
                items = items,
                total = total,
                discount = discount,
                finalTotal = total - discount,
                voucherCode = appliedCode // Trả về để UI hiển thị "Đã áp dụng mã ABC"
            });
        }

        // 2. THÊM VÀO GIỎ
        [HttpPost]
        public IActionResult AddToCart(int id, int qty = 1)
        {
            int uId = GetUserId();
            if (uId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (qty <= 0) qty = 1;

            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null)
            {
                item.Quantity += qty;
            }
            else
            {
                _context.Carts.Add(new Cart { ProductId = id, UserId = uId, Quantity = qty });
            }

            var product = _context.Products.Find(id);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                productName = product?.ProductName ?? "Sản phẩm",
                productImage = product?.Image ?? "default.png",
                quantity = (item != null ? item.Quantity : 1)
            });
        }

        // 2b. MUA NGAY (Chỉ thanh toán sản phẩm này, không gộp cả giỏ hàng)
        public async Task<IActionResult> BuyNow(int productId)
        {
            int uId = GetUserId();
            if (uId == 0) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return RedirectToAction("Index", "Home");

            // Tạo đơn hàng mới cho RIÊNG sản phẩm này
            var order = new Order
            {
                UserId = uId,
                CreatedAt = DateTime.Now,
                TotalAmount = product.Price,
                DiscountAmount = 0,
                FinalAmount = product.Price,
                PaymentMethod = 1,
                PaymentStatus = 0,
                OrderStatus = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var detail = new OrderDetail
            {
                OrderId = order.OrderId,
                ProductId = productId,
                Quantity = 1,
                Price = product.Price
            };
            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "Order", new { id = order.OrderId, isConfirm = true });
        }

        // 3. TĂNG SỐ LƯỢNG
        [HttpPost]
        public IActionResult Increase(int id)
        {
            int uId = GetUserId();
            var item = _context.Carts.FirstOrDefault(x => x.ProductId == id && x.UserId == uId);
            if (item != null)
            {
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
            if (item != null)
            {
                _context.Carts.Remove(item);
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }

        // 6. ÁP DỤNG VOUCHER (Lưu vào Session)
        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            int uId = GetUserId();
            if (uId == 0) return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng voucher!" });

            var codeNormalized = code.Trim().ToUpperInvariant();
            var voucher = _context.Vouchers.FirstOrDefault(v => v.Code.ToUpper() == codeNormalized && v.Status == "Active");
            if (voucher == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });

            if (DateTime.Now < voucher.StartDate || DateTime.Now > voucher.EndDate)
                return Json(new { success = false, message = "Voucher không trong thời gian sử dụng!" });

            if (voucher.UsedCount >= voucher.UsageLimit)
                return Json(new { success = false, message = "Voucher đã hết lượt sử dụng!" });

            var cartItems = _context.Carts.Include(x => x.Product).Where(x => x.UserId == uId).ToList();
            var total = cartItems.Sum(x => x.Product != null ? x.Product.Price * x.Quantity : 0);
            if (total < voucher.MinOrderValue)
                return Json(new { success = false, message = $"Voucher này chỉ áp dụng cho đơn từ {voucher.MinOrderValue.ToString("N0")}đ trở lên." });

            HttpContext.Session.SetString("VoucherCode", code);
            HttpContext.Session.SetInt32("VoucherAppliedManual", 1);
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetVoucherSuggestions(string query = "")
        {
            int uId = GetUserId();
            var cartItems = new List<Cart>();
            if (uId > 0)
            {
                cartItems = _context.Carts.Include(x => x.Product).Where(x => x.UserId == uId).ToList();
            }

            var total = cartItems.Sum(x => x.Product != null ? x.Product.Price * x.Quantity : 0);
            var queryNormalized = (query ?? string.Empty).Trim().ToUpperInvariant();

            var vouchers = _context.Vouchers
                .Where(v => v.Status == "Active"
                         && DateTime.Now >= v.StartDate
                         && DateTime.Now <= v.EndDate
                         && v.UsedCount < v.UsageLimit
                         && (string.IsNullOrEmpty(queryNormalized)
                              || v.Code.ToUpper().Contains(queryNormalized)
                              || (!string.IsNullOrEmpty(v.Name) && v.Name.ToUpper().Contains(queryNormalized))))
                .OrderBy(v => v.MinOrderValue)
                .ThenBy(v => v.Code)
                .Take(10)
                .Select(v => new
                {
                    v.Code,
                    v.Name,
                    MinTotal = v.MinOrderValue,
                    IsEligible = total >= v.MinOrderValue,
                    v.DiscountType,
                    v.DiscountValue
                })
                .ToList();

            return Json(vouchers);
        }

        // 7. THANH TOÁN (TỪ GIỎ HÀNG SANG ĐƠN HÀNG - Đã gộp tính Voucher)
        // ... giữ nguyên namespace và các hàm GetUserId ...

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            int uId = GetUserId();
            var cartItems = _context.Carts.Include(x => x.Product).Where(x => x.UserId == uId).ToList();
            if (!cartItems.Any()) return Json(new { success = false, message = "Giỏ hàng rỗng" });

            decimal totalAmount = cartItems.Sum(x => x.Product.Price * x.Quantity);
            string voucherCode = HttpContext.Session.GetString("VoucherCode");
            decimal discountAmount = 0;
            int? voucherId = null;

            // 1. Kiểm tra lại voucher do người dùng đã áp dụng
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code.ToUpper() == voucherCode.ToUpper() && v.Status == "Active");
                if (voucher != null && totalAmount >= voucher.MinOrderValue && voucher.UsedCount < voucher.UsageLimit)
                {
                    discountAmount = (voucher.DiscountType == 1) ? (totalAmount * voucher.DiscountValue / 100) : voucher.DiscountValue;
                    voucherId = voucher.VoucherId;
                    voucher.UsedCount++; // Tăng lượt dùng
                }
                else
                {
                    HttpContext.Session.Remove("VoucherCode");
                }
            }

            // 2. Tạo Đơn hàng (Order)
            var order = new Order
            {
                UserId = uId,
                CreatedAt = DateTime.Now,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                FinalAmount = totalAmount - discountAmount,
                OrderStatus = 1,
                PaymentStatus = 0,
                VoucherId = voucherId
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy OrderId

            // 3. QUAN TRỌNG: Chuyển từ Cart sang OrderDetail
            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price // Lưu giá tại thời điểm mua
                });
            }

            // 4. Dọn dẹp
            _context.Carts.RemoveRange(cartItems);
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("VoucherAppliedManual");
            await _context.SaveChangesAsync();

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
                .AsNoTracking()
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
                .Include(o => o.User)
                .Include(o => o.Voucher)
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

        // AUTO-CONFIRM PAYMENT (Simulated AI Detection)
        [HttpPost]
        public async Task<IActionResult> ConfirmPaymentAuto(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.PaymentStatus = 1; // 1: Đã thanh toán
                order.OrderStatus = 2;   // 2: Đang chuẩn bị (Tự động chuyển tiếp trạng thái)
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
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


    public class ReviewSubmission
    {
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }


}
