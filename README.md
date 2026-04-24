# BoomMat E-Commerce & Cafe Management

Dự án Hệ thống Quản lý và Bán hàng trực tuyến (E-commerce) được phát triển trên kiến trúc **MVC (Model-View-Controller)** kết hợp với Entity Framework Core. Hệ thống mang lại giá trị cốt lõi là tối ưu hóa luồng đặt hàng trực tuyến, xử lý thanh toán, áp dụng khuyến mãi thông minh, và cung cấp Dashboard quản trị toàn diện cho doanh nghiệp.

## Công nghệ sử dụng (Tech Stack)

- **Backend:**  C# (.NET 9.0), ASP.NET Core MVC
- **Database:**  Microsoft SQL Server, Entity Framework Core 9.0 (Code-First)
- **Frontend:**  HTML5, CSS3, JavaScript, Razor Views
- **Bảo mật & State:**  Cookie Authentication, Session Memory, Swagger API

## Tính năng nổi bật (Key Features)

- **Giỏ hàng thông minh**: Quản lý thêm/bớt/sửa sản phẩm bằng Session kết hợp Database. Hỗ trợ chức năng "Mua ngay" (Buy Now) giúp tạo đơn hàng độc lập mà không làm thay đổi các món đồ đang có sẵn trong giỏ hàng chung.
- **Hệ thống Voucher nâng cao**: Kiểm soát điều kiện áp mã một cách chặt chẽ (hạn sử dụng, giới hạn lượt dùng, giá trị đơn tối thiểu). Ngăn chặn gian lận bằng logic 1 user chỉ dùng 1 mã 1 lần. Tự động tính toán chiết khấu % hoặc số tiền cố định.
- **Báo cáo doanh thu (Admin Dashboard)**: Thống kê trực quan số lượng đơn hàng, doanh số và tự động tổng hợp top 5 sản phẩm bán chạy nhất theo từng mốc thời gian (ngày, tuần, tháng, năm).
- **Đánh giá sản phẩm đa chiều**: Khách hàng có thể để lại bình luận và đánh giá số sao (1-5) cho từng sản phẩm riêng lẻ trong một hóa đơn. Lưu trữ rõ ràng theo `OrderDetail` để đảm bảo tính minh bạch.
- **Quản lý & Phân quyền an toàn**: Phân quyền chi tiết hệ thống (Admin, Staff, Customer). Tích hợp thuật toán "Xóa mềm" (Soft-delete): Khóa tài khoản hoặc ẩn sản phẩm nếu đã có dữ liệu giao dịch liên quan, ngăn ngừa việc gãy vỡ cấu trúc dữ liệu kế toán.

## Kiến trúc & Cơ sở dữ liệu (Architecture / Database)

- Hệ thống được cấu trúc theo chuẩn **ASP.NET Core MVC**, tách bạch rõ ràng phần hiển thị (Views) và logic điều khiển (Controllers như `CartController`, `AdminController`, `OrderController`). 
- Dữ liệu liên kết chặt chẽ bằng Entity Framework: Người dùng (`Users`), Sản phẩm (`Products`), Danh mục (`Categories`), Giỏ hàng (`Carts`), Đơn hàng (`Orders`, `OrderDetails`), Đánh giá (`Reviews`) và Khuyến mãi (`Vouchers`). Cơ chế **Seed Data** được triển khai sẵn trong `Program.cs` để tự động khởi tạo database và tài khoản mẫu ngay trong lần chạy đầu tiên.

## Giao diện sản phẩm (Screenshots)

<img width="1898" height="971" alt="image" src="https://github.com/user-attachments/assets/6029fe98-70cd-484d-b759-dfb1cde6230c" />

## Hướng dẫn cài đặt & Chạy dự án (How to Run)

1. **Clone mã nguồn** và mở thư mục chứa file `.csproj` bằng IDE của bạn (Visual Studio / VS Code).
2. **Cấu hình Database**: Mở file `appsettings.json`, chỉnh sửa chuỗi kết nối `DefaultConnection` phù hợp với máy chủ SQL Server của bạn. Ví dụ:
   ```json
   "DefaultConnection": "Server=TÊN_MÁY_CỦA_BẠN;Database=ASM;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=60;"
   ```
3. **Khôi phục thư viện**: Mở Terminal và chạy lệnh sau để cài đặt các package cần thiết:
   ```bash
   dotnet restore
   ```
4. **Cập nhật Database**: Lệnh này sẽ tự động tạo cấu trúc bảng và đổ dữ liệu mẫu vào SQL Server:
   ```bash
   dotnet ef database update
   ```
5. **Khởi chạy ứng dụng**: 
   ```bash
   dotnet run
   ```
   *Lưu ý: Quá trình Seed Data đã tự động tạo sẵn một tài khoản Admin (`admin@gmail.com` / `123`) để bạn có thể trải nghiệm ngay lập tức.*
