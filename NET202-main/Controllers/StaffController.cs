using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASM.Data;
using ASM.Models;
using ASM.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace ASM.Controllers
{
    // ===== CÁC LỚP DTO (DỮ LIỆU TRUYỀN TẢI) =====
    public class PosOrderDto
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public List<int> Quantities { get; set; } = new List<int>();
        public decimal TotalAmount { get; set; }
        public int PaymentMethod { get; set; } 
        public string? Note { get; set; }
    }

    public class RecipeUpdateDto
    {
        public int ProductId { get; set; }
        public string Description { get; set; }
    }

    public class ProductStatusDto
    {
        public int ProductId { get; set; }
        public string Status { get; set; }
    }

    // ===== CONTROLLER CHÍNH =====
    [Authorize(Roles = "Staff,Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // QUẢN LÝ ĐƠN HÀNG (CHUNG)
        // ==========================================================
        
        public IActionResult Order()
        {
            return RedirectToAction("CreateOfflineOrder");
        }

        public IActionResult OrderDetail(int id)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Voucher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();
            return PartialView(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int orderStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.OrderStatus < 5) 
            {
                order.OrderStatus = orderStatus;
                
                // Nếu hoàn thành đơn (Trạng thái 4), tự động chốt là Đã thanh toán
                if (orderStatus == 4) 
                {
                    order.PaymentStatus = 1; 
                }
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("OnlineOrder");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId, string cancelReason)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.OrderStatus < 4) 
            {
                if (string.IsNullOrWhiteSpace(cancelReason))
                {
                    TempData["Error"] = "Vui lòng nhập lý do hủy đơn.";
                    return RedirectToAction("OnlineOrder");
                }
                order.OrderStatus = 5; 
                order.CancelReason = cancelReason;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã hủy đơn hàng #ORD-{order.OrderId:D4}.";
            }
            return RedirectToAction("OnlineOrder");
        }

        // ==========================================================
        // MÀN HÌNH THEO DÕI ĐƠN HÀNG
        // ==========================================================
        
        public async Task<IActionResult> OnlineOrder()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderType == 1)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OfflineOrder()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderType == 2)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        // ==========================================================
        // QUẢN LÝ THỰC ĐƠN (MENU CHUNG)
        // ==========================================================
        
        public async Task<IActionResult> Menu()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Category!.CategoryName)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = product.Status == "Active" ? "Hidden" : "Active";
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Menu");
        }

        public IActionResult ProductDetail(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null) return NotFound();
            return PartialView(product);
        }

        // ==========================================================
        // POS TERMINAL (BÁN HÀNG TẠI QUẦY)
        // ==========================================================

        [HttpGet]
public async Task<IActionResult> CreateOfflineOrder(int? categoryId, string? search)
{
    ViewBag.Categories = await _context.Categories.Where(c => c.Status == "Active").ToListAsync();
    ViewBag.CurrentCategory = categoryId;
    ViewBag.SearchTerm = search;

    // Lấy danh sách sản phẩm (chỉ ẩn những món Hidden hoàn toàn)
    var query = _context.Products.Include(p => p.Category)
                        .Where(p => p.Status == "Active" || p.Status == "Inactive").AsQueryable();

    if (categoryId.HasValue && categoryId > 0)
    {
        query = query.Where(p => p.CategoryId == categoryId);
    }

    var products = await query.ToListAsync();

    // CHUẨN HÓA TÌM KIẾM THÔNG MINH (Không dấu, không khoảng trắng)
    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchNormalized = Utilities.ToUnaccent(search);
        products = products.Where(p => Utilities.ToUnaccent(p.ProductName).Contains(searchNormalized)).ToList();
    }

    return View(products);
}

        [HttpPost]
        public async Task<IActionResult> SubmitPosOrder([FromBody] PosOrderDto request)
        {
            if (request.ProductIds == null || !request.ProductIds.Any())
            {
                return BadRequest(new { success = false, message = "Giỏ hàng đang trống!" });
            }

            var newOrder = new Order
            {
                UserId = 1, // ID của nhân viên hoặc khách vô danh
                OrderType = 2, 
                OrderStatus = 1, 
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == 1 ? 1 : 0, 
                TotalAmount = request.TotalAmount,
                FinalAmount = request.TotalAmount,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync(); 

            for (int i = 0; i < request.ProductIds.Count; i++)
            {
                if (request.Quantities[i] > 0)
                {
                    var product = await _context.Products.FindAsync(request.ProductIds[i]);
                    if (product != null)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = newOrder.OrderId,
                            ProductId = request.ProductIds[i],
                            Quantity = request.Quantities[i],
                            Price = product.Price
                        };
                        _context.OrderDetails.Add(orderDetail);
                    }
                }
            }
            await _context.SaveChangesAsync();

            if (request.PaymentMethod == 1) 
            {
                return Ok(new { 
                    success = true, 
                    isQr = false,
                    message = $"Thanh toán tiền mặt thành công đơn #ORD-{newOrder.OrderId:D4}!", 
                    orderId = newOrder.OrderId 
                });
            }
            else 
            {
                string bankBin = "970436"; 
                string bankAccount = "1234567890"; 
                string accountName = "BREAD AND BREW";
                string orderInfo = $"Thanh toan don {newOrder.OrderId}";
                string qrUrl = $"https://img.vietqr.io/image/{bankBin}-{bankAccount}-compact.png?amount={request.TotalAmount}&addInfo={orderInfo}&accountName={accountName}";

                return Ok(new { 
                    success = true, 
                    isQr = true, 
                    qrUrl = qrUrl,
                    orderId = newOrder.OrderId,
                    message = "Vui lòng quét mã QR để hoàn tất thanh toán." 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmQrPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = 1; 
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Nhận tiền thành công!" });
            }
            return NotFound();
        }

        // ==========================================================
        // CẬP NHẬT CÔNG THỨC & TRẠNG THÁI SẢN PHẨM (AJAX)
        // ==========================================================

        // ==========================================================
// CẬP NHẬT CÔNG THỨC & TRẠNG THÁI SẢN PHẨM (AJAX)
// ==========================================================

[HttpPost]
public async Task<IActionResult> UpdateRecipe([FromBody] RecipeUpdateDto request)
{
    try
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

        product.Description = request.Description;
        product.UpdatedAt = DateTime.Now;
        
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}

// ĐÃ SỬA: Đổi tên thành ToggleProductStatusAjax để không bị trùng với hàm cũ
[HttpPost]
public async Task<IActionResult> ToggleProductStatusAjax([FromBody] ProductStatusDto request)
{
    try
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

        product.Status = request.Status;
        product.UpdatedAt = DateTime.Now;
        
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
    }
}