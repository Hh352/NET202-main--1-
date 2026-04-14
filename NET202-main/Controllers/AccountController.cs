using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using ASM.Models; 
using ASM.Data; 
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace ASM.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================= PHẦN ĐĂNG NHẬP =======================
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                if (user.Status == "Locked")
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.";
                    return View();
                }

                // 1. Phân quyền
                string roleName = "User"; // Role = 0
                if (user.Role == 1) roleName = "Staff";   
                else if (user.Role == 2) roleName = "Admin"; 

                // 2. Nạp thông tin vào túi (Claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName), 
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()) 
                };

                // 3. Khai báo định danh
                var claimsIdentity = new ClaimsIdentity(
                    claims, 
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name, 
                    ClaimTypes.Role  
                );

                // 4. Thiết lập Cookie (Giữ đăng nhập khi tắt Server)
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, 
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Tăng lên 30 ngày cho thoải mái
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );
                
                // Lưu Avatar vào session để hiển thị lên Header
                HttpContext.Session.SetString("Avatar", user.Avatar ?? "/Images/image.png");

                // 5. Điều hướng 
                if (user.Role == 2) return RedirectToAction("Report", "Admin");
                // ĐÃ SỬA: Chuyển hướng Staff về CreateOfflineOrder thay vì Order
                else if (user.Role == 1) return RedirectToAction("CreateOfflineOrder", "Staff");
                else return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
            return View();
        }

        // ======================= PHẦN ĐĂNG KÝ =======================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string FullName, string Email, string Phone, string Password)
        {
            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Phone))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                return View();
            }

            // KIỂM TRA TRÙNG EMAIL TẠI ĐÂY
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existingUser != null)
            {
                ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn Email khác!";
                return View();
            }

            var newUser = new User
            {
                FullName = FullName,
                Email = Email,
                Phone = Phone, // Lưu số điện thoại vào Database
                Password = Password, 
                Role = 0, 
                Status = "Active", 
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }

        // ======================= ĐĂNG XUẤT =======================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // 1. Xóa Cookie xác thực chính yếu của hệ thống
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // 2. Xóa sạch mọi dữ liệu trong Session
            HttpContext.Session.Clear();

            // 3. Quét và ép xóa tất cả các Cookie liên quan đang bị kẹt trên trình duyệt
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // 4. Điều hướng thẳng về TRANG CHỦ (Home)
            return RedirectToAction("Index", "Home");
        }

        // ======================= TỪ CHỐI TRUY CẬP =======================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content("LỖI: Bạn không có quyền truy cập vào khu vực này.");
        }
    }
}