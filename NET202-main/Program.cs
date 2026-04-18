using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ASM.Models;
using ASM.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromDays(30); 
        options.SlidingExpiration = true;
    });

// Add services to the container (Có thêm cấu hình IgnoreCycles để API không bị lỗi JSON lặp vòng)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =========================================================================
// [THÊM MỚI] CẤU HÌNH SESSION (Bắt buộc để Giỏ hàng Online hoạt động)
// =========================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian sống của Session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- BƯỚC 1: THÊM DỊCH VỤ SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // --- BƯỚC 2: KÍCH HOẠT MIDDLEWARE SWAGGER TRONG MÔI TRƯỜNG DEV ---
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASM API V1");
        // Bỏ comment dòng dưới nếu muốn trang Swagger là trang chủ mặc định khi chạy web
        // c.RoutePrefix = string.Empty; 
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// =========================================================================
// [THÊM MỚI] KÍCH HOẠT MIDDLEWARE SESSION 
// (Lưu ý quan trọng: Phải đặt dưới UseRouting và trên UseAuthentication)
// =========================================================================
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================================
// [TỐI ƯU] TỰ ĐỘNG KHỞI TẠO DATABASE VÀ DỮ LIỆU (SEED DATA)
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    try 
    {
        // 1. Tự động tạo Database và áp dụng các bản cập nhật (Migration)
        context.Database.Migrate();

        // 2. Tạo tài khoản Admin mẫu
        if (!context.Users.Any(u => u.Email == "admin@gmail.com"))
        {
            context.Users.Add(new User
            {
                FullName = "Administrator",
                Email = "admin@gmail.com",
                Password = "123",
                Role = 2, // Admin
                Status = "Active",
                CreatedAt = DateTime.Now
            });
        }

        // 3. Tạo tài khoản Khách hàng mẫu (Để test Gift Box)
        if (!context.Users.Any(u => u.Email == "user@gmail.com"))
        {
            context.Users.Add(new User
            {
                FullName = "Test Customer",
                Email = "user@gmail.com",
                Password = "123",
                Role = 1, // Customer
                Status = "Active",
                CreatedAt = DateTime.Now // Tài khoản mới sẽ hiện Gift Box ngay!
            });
        }
        
        context.SaveChanges();

        // 🚀 GỌI SEED DATA CHO CÁC THÀNH PHẦN KHÁC
        ASM.Data.DbSeeder.SeedMenuData(context);
        ASM.Data.DbSeeder.SeedReviewsData(context);
        ASM.Data.DbSeeder.SeedVouchersData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi khởi tạo dữ liệu mẫu.");
    }
}

app.Run();