using Microsoft.AspNetCore.Mvc;
using ASM.Data;
using ASM.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ASM.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị Profile
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            if (!int.TryParse(userIdStr, out int id))
                return BadRequest("ID người dùng không hợp lệ.");

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Lưu thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(User model, IFormFile? avatarFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int id))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Address = model.Address;

            // Xử lý Upload Avatar
            if (avatarFile != null && avatarFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatarFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
                user.Avatar = "/Images/" + fileName;
                HttpContext.Session.SetString("Avatar", user.Avatar);
            }

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                ViewBag.Message = "Cập nhật thành công!";

                // ==========================================
                // LÀM MỚI COOKIE ĐỂ ĐỔI TÊN NGAY LẬP TỨC
                // ==========================================
                string roleName = user.Role == 2 ? "Admin" : (user.Role == 1 ? "Staff" : "User");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi lưu: " + ex.Message;
            }

            return View(user);
        }
    }
}