using ASM.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM.Data
{
    public static class DbSeeder
    {
        public static void SeedMenuData(ApplicationDbContext context)
        {
            // Kiểm tra xem đã có đủ menu chưa (ví dụ trên 40 món)
            // Nếu đã có nhiều hơn 40 món thì không seed nữa để tránh lặp lộn xộn
            if (context.Products.Count() > 40) return;

            // 1. CHUẨN BỊ DANH MỤC (CATEGORIES)
            var categoryNames = new string[] 
            { 
                "Cà phê truyền thống",  // 1
                "Trà & Thức uống trái cây", // 2
                "Đá xay (Freeze)",      // 3
                "Bánh ngọt & Pastry",     // 4
                "Bánh mì & Đồ mặn",     // 5
                "Nước ép & Sinh tố",    // 6
                "Cà phê đóng gói",      // 7
                "Merchandise & Quà tặng"// 8
            };

            var categoryDict = new Dictionary<string, int>();

            foreach (var name in categoryNames)
            {
                var cat = context.Categories.FirstOrDefault(c => c.CategoryName == name);
                if (cat == null)
                {
                    cat = new Category { CategoryName = name, Status = "Active" };
                    context.Categories.Add(cat);
                    context.SaveChanges(); // Lưu để lấy ID
                }
                categoryDict[name] = cat.CategoryId;
            }

            // 2. CHUẨN BỊ MÓN ĂN (PRODUCTS)
            var products = new List<Product>();

            // --- 2.1 CÀ PHÊ TRUYỀN THỐNG ---
            var coffeeList = new List<(string, decimal, string)> 
            {
                ("Phin Sữa Đá Nguyên Bản", 29000, "Cà phê Robusta rang mộc pha phin cùng sữa đặc"),
                ("Cà Phê Đen Đá", 25000, "Cà phê đen nguyên bản đậm chất Việt"),
                ("Bạc Xỉu Đá", 35000, "Nhiều sữa đặc, một chút cà phê. Dành cho người thích ngọt"),
                ("Phin Đen Nóng", 25000, "Cà phê đen pha phin nóng"),
                ("Phin Sữa Nóng", 29000, "Cà phê sữa pha phin nón truyền thống"),
                ("Espresso Nóng", 39000, "Chiết xuất 1 short Espresso đậm đặc"),
                ("Americano Đá", 45000, "Espresso pha loãng với nước đá"),
                ("Latte Nóng", 55000, "Espresso và nhiều sữa tươi đánh bọt nhẹ"),
                ("Cappuccino Nóng", 55000, "Espresso, sữa tươi và rất nhiều bọt sữa"),
                ("Caramel Macchiato", 65000, "Cà phê sữa tươi với sốt Caramel đặc biệt"),
                ("Mocha Đá", 65000, "Cà phê kết hợp với sữa tươi và sốt Socola đen"),
                ("Cold Brew Truyền Thống", 55000, "Cà phê ủ lạnh 24h, dịu nhẹ và trái cây"),
                ("Cold Brew Sữa Tươi", 60000, "Cà phê ủ lạnh kết hợp sữa tươi"),
                ("Cold Brew Trái Cây", 65000, "Cà phê ủ lạnh mix cùng trái cây mát lạnh")
            };
            foreach (var p in coffeeList) AddProduct(products, p.Item1, p.Item2, categoryDict["Cà phê truyền thống"], p.Item3, "https://images.unsplash.com/photo-1541167760496-1628856ab772?q=80&w=600");

            // --- 2.2 TRÀ & THỨC UỐNG TRÁI CÂY ---
            var teaList = new List<(string, decimal, string)>
            {
                ("Trà Đào Cam Sả", 49000, "Best seller! Vị trà đậm kết hợp đào ngọt, cam chua và sả thơm"),
                ("Trà Thanh Đào", 45000, "Trà ô long thanh mát hương đào"),
                ("Trà Sen Vàng", 49000, "Trà ô long với hạt sen bùi béo và bọt sữa mềm"),
                ("Trà Lài Đác Thơm", 49000, "Hương lài thơm ngát kết hợp hạt đác và mứt dứa thơm"),
                ("Trà Đen Macchiato", 45000, "Trà đen nguyên bản với lớp kem mặn Macchiato béo ngậy"),
                ("Matcha Latte", 55000, "Bột trà xanh Nhật Bản hòa quyện cùng sữa tươi"),
                ("Trà Xanh Đường Hồng", 45000, "Trà xanh thanh tao kết hợp syrub đường hồng"),
                ("Hồng Trà Sữa Thạch Trân Châu", 49000, "Trà sữa truyền thống kèm trân châu đen"),
                ("Trà Sữa Oolong Nướng", 55000, "Vị trà oolong nướng thơm đặc trưng kết hợp sữa béo"),
                ("Oolong Vải Hoa Hồng", 55000, "Trà oolong nhẹ nhàng kết hợp vị vải ngọt và hương nụ hồng"),
                ("Trà Vải Nhiệt Đới", 55000, "Trà trái cây vị vải dành cho ngày hè"),
                ("Trà Xoài Đác", 55000, "Trà xanh mix xoài tươi và hạt đác giòn")
            };
            foreach (var p in teaList) AddProduct(products, p.Item1, p.Item2, categoryDict["Trà & Thức uống trái cây"], p.Item3, "https://images.unsplash.com/photo-1556881286-fc6915169721?q=80&w=600");

            // --- 2.3 ĐÁ XAY (FREEZE) ---
            var freezeList = new List<(string, decimal, string)>
            {
                ("Caramel Phin Freeze", 59000, "Cà phê đá xay cùng sốt caramel và thạch cafe"),
                ("Classic Phin Freeze", 55000, "Phiên bản cà phê đá xay truyền thống"),
                ("Freeze Trà Xanh (Matcha)", 65000, "Đá xay từ bột Matcha thanh tao và lớp kem tươi"),
                ("Freeze Socola", 59000, "Đá xay đậm vị cacao ngọt đắng cùng kem béo"),
                ("Freeze Bánh Quy Sữa", 65000, "Đá xay cùng bánh quy oreo giòn rụm"),
                ("Freeze Sữa Dừa", 59000, "Vị sữa tươi béo ngậy kết hợp hương dừa đá xay"),
                ("Cà Phê Dừa Đá Xay", 65000, "Cà phê đậm vị mix cốt dừa siêu béo")
            };
            foreach (var p in freezeList) AddProduct(products, p.Item1, p.Item2, categoryDict["Đá xay (Freeze)"], p.Item3, "https://images.unsplash.com/photo-1572490122747-3968b75cc699?q=80&w=600");

            // --- 2.4 BÁNH NGỌT & PASTRY ---
            var sweetList = new List<(string, decimal, string)>
            {
                ("Bánh Sừng Bò (Croissant) Truyyền thống", 29000, "Bánh sừng bò Pháp nghìn lớp giòn tan"),
                ("Croissant Phô Mai Lạp Xưởng", 45000, "Bánh croissant với nhân mặn đặc biệt phô mai lạp xưởng"),
                ("Bánh Mousse Trà Xanh Matcha", 39000, "Bánh mousse mát lạnh vị matcha đậm trà"),
                ("Tiramisu Cổ Điển", 45000, "Bánh Tiramisu Ý với cốt bánh lady finger tẩm cà phê"),
                ("Cheesecake Chanh Dây", 49000, "Bánh phô mai nướng với mứt chanh dây chua chua ngọt ngọt"),
                ("Red Velvet Cake", 49000, "Bánh velvet đỏ kiêu kỳ với lớp kem creamcheese mịn màng"),
                ("Brownie Socola Cổ Điển", 39000, "Bánh brownie siêu đậm vị socola đen nguyên chất"),
                ("Bánh Su Kem Vanilla (Hộp 3 cái)", 45000, "Bánh su kem mềm lạnh thơm béo hương vani tự nhiên"),
                ("Tart Trứng nướng", 25000, "Bánh tart trứng sữa nướng vỏ giòn xốp"),
                ("Mousse Chanh Dây", 39000, "Bánh mousse chanh dây chua ngọt tươi mát"),
                ("Macaron Vị Raspberry", 25000, "Bánh macaron phong cách Pháp vị mâm xôi"),
                ("Macaron Vị Chocolate", 25000, "Bánh macaron nhân chocolate ganache thơm lừng")
            };
            foreach (var p in sweetList) AddProduct(products, p.Item1, p.Item2, categoryDict["Bánh ngọt & Pastry"], p.Item3, "https://images.unsplash.com/photo-1578985545062-69928b1d9587?q=80&w=600");

            // --- 2.5 BÁNH MÌ & ĐỒ MẶN ---
            var savoryList = new List<(string, decimal, string)>
            {
                ("Bánh Mì Thịt Nướng", 35000, "Bánh mì Việt Nam giòn rụm với thịt nướng thơm lừng"),
                ("Bánh Mì Chả Lụa Xíu Mại", 39000, "Bánh mì kẹp chả lụa và xíu mại xốt cà đậm đà"),
                ("Bánh Bao Truyền Thống", 25000, "Bánh bao nhân thịt trứng cút nóng hổi"),
                ("Croissant Trứng Gà Phô Mai", 49000, "Bánh sừng bò nhân kẹp trứng chưng phô mai béo ngậy"),
                ("Sandwich Gà Áp Chảo", 45000, "Sandwich mềm kẹp lườn gà áp chảo ăn kiêng"),
                ("Sandwich Cá Ngừ Mayonnaise", 45000, "Sandwich lúa mạch nhân cá ngừ vị xốt chua nhẹ"),
                ("Bánh Mì Que Hải Phòng Pâté Cay", 18000, "Bánh mì que đặc sản kèm patê siêu béo, xốt ớt cay")
            };
            foreach (var p in savoryList) AddProduct(products, p.Item1, p.Item2, categoryDict["Bánh mì & Đồ mặn"], p.Item3, "https://images.unsplash.com/photo-1550507992-eb63ffee0224?q=80&w=600");

            // --- 2.6 NƯỚC ÉP & SINH TỐ ---
            var juiceList = new List<(string, decimal, string)>
            {
                ("Nước Ép Cam Tươi", 49000, "100% cam ép tươi tự nhiên không thêm đường"),
                ("Nước Ép Dưa Hấu", 45000, "Dưa hấu tươi mát lạnh giải khát ngày hè"),
                ("Nước Ép Táo Cần Tây", 55000, "Nước ép healthy detox cơ thể cực kỳ hiệu quả"),
                ("Sinh Tố Bơ (Theo mùa)", 59000, "Bơ Đắk Lắk dẻo béo mix sữa đặc"),
                ("Sinh Tố Xoài Chanh Dây", 55000, "Vị ngọt của xoài nguyên chất hòa quyện cùng chanh dây chua chua"),
                ("Sữa Chua Trái Cây Đánh Đá", 45000, "Sữa chua lên men tự nhiên kết hợp thạch trái cây giòn dai"),
                ("Nước Ép Dứa Cà Rốt", 49000, "Nước ép thanh mát cung cấp nhiều nguyên tố vi lượng"),
                ("Nước Đá Me", 25000, "Đá me sên đường mộc mạc thơm lừng miền Tây")
            };
            foreach (var p in juiceList) AddProduct(products, p.Item1, p.Item2, categoryDict["Nước ép & Sinh tố"], p.Item3, "https://images.unsplash.com/photo-1600271886742-f049cd451bba?q=80&w=600");

            // --- 2.7 CÀ PHÊ ĐÓNG GÓI ---
            var packList = new List<(string, decimal, string)>
            {
                ("Cà Phê Rang Xay Robusta Premium 500g", 120000, "100% hạt Robusta rang đậm, hậu vị đắng mạnh"),
                ("Cà Phê Rang Xay Arabica Cầu Đất 250g", 150000, "Hạt Arabica rang vừa, tính chua thanh, hương hoa ngây ngất"),
                ("Cà Phê Hòa Tan 3in1 Hộp 18 Gói", 85000, "Pha sẵn thơm ngon, tiết kiệm thời gian buổi sáng"),
                ("Bạc Xỉu Sữa Hộp 12 Gói", 95000, "Chuyên dành cho phái nữ, đậm vị sữa ngào ngạt"),
                ("Cà Phê Phin Giấy 5 Gói", 75000, "Pour over tại nhà chưa bao giờ dễ dàng đến thế")
            };
            foreach (var p in packList) AddProduct(products, p.Item1, p.Item2, categoryDict["Cà phê đóng gói"], p.Item3, "https://images.unsplash.com/photo-1559525839-b184a4d698c7?q=80&w=600");

            // --- 2.8 MERCHANDISE & QUÀ TẶNG ---
            var merchList = new List<(string, decimal, string)>
            {
                ("Cốc Sứ Mang Đi (Tumbler)", 250000, "Giúp thức uống của bạn nóng/lạnh bền bỉ qua thời gian (giảm 10k nếu dùng tại quán)"),
                ("Túi Vải Canvas B&B", 120000, "Túi Tote Canvas thân thiện môi trường, phong cách Minimalist"),
                ("Bình Giữ Nhiệt 500ml", 350000, "Bình giữ nhiệt Inox 304 cao cấp giữ nóng 6h, lạnh 12h"),
                ("Sổ Tay Planner B&B", 85000, "Sổ tay cà phê vintage ruột chấm bi dot grid"),
                ("Móc Khóa Ly Cà Phê", 35000, "Phụ kiện móc khóa bé xinh cho balo của bạn"),
                ("Phin Nhôm Pha Cà Phê Cao Cấp", 70000, "Phin nhôm Anode tản nhiệt tốt, chiết xuất cà phê siêu đỉnh"),
                ("Ly Thủy Tinh Khói Mờ", 65000, "Ly decor quai cầm hiện đại tối giản")
            };
            foreach (var p in merchList) AddProduct(products, p.Item1, p.Item2, categoryDict["Merchandise & Quà tặng"], p.Item3, "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?q=80&w=600");

            // LƯU TOÀN BỘ VÀO DB
            if (products.Any())
            {
                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }

        private static void AddProduct(List<Product> list, string name, decimal price, int catId, string desc, string imgUrl)
        {
            list.Add(new Product
            {
                ProductName = name,
                Price = price,
                CategoryId = catId,
                Description = desc,
                Image = imgUrl,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        public static void SeedReviewsData(ApplicationDbContext context)
        {
            // Nếu đã từng gen ra số lượng cực lớn (>3000, tức là đã chạy data sau khi chia lại tỷ trọng) thì ngừng
            // Còn nếu số lượng cũ ~1500 (tầm 15-40 review đều nhau đợt trước) thì vẫn pass qua để xóa làm lại.
            if (context.Reviews.Count() > 3000) return;

            // 1. CHUẨN BỊ USER ẢO ĐỂ ĐÁNH GIÁ (NẾU CHƯA CÓ)
            var uList = context.Users.Where(u => u.Email.Contains("fakeuser")).ToList();
            if (uList.Count < 5)
            {
                for (int i = 1; i <= 10; i++)
                {
                    context.Users.Add(new User
                    {
                        FullName = $"Khách hàng {i}",
                        Email = $"fakeuser{i}@gmail.com",
                        Password = "123",
                        Role = 0,
                        Status = "Active",
                        CreatedAt = DateTime.Now
                    });
                }
                context.SaveChanges();
                uList = context.Users.Where(u => u.Email.Contains("fakeuser")).ToList();
            }

            var random = new Random();
            var products = context.Products.Include(p => p.Category).ToList();

            // CÁC BỘ BÌNH LUẬN THEO NHÓM
            var drinkComments = new string[] {
                "Nước rất đậm đà, vị ngon tuyệt vời!", "Đúng gu thích uống đậm của mình, 10 điểm.", "Hương vị rất đặc trưng, không quá ngọt.", 
                "Mùi thơm ngây ngất, đóng gói mang về cực kỳ cẩn thận.", "Nước uống ngon, đậm vị, trân châu dai giòn.", "Mình uống món này mãi không chán.",
                "Công thức pha chế của quán đỉnh quá.", "Lần nào tới cũng order món này, recommend mọi người thử.", "Rất thơm ngon, sẽ tiếp tục ủng hộ.",
                "Chất lượng tốt so với tầm giá, vị thanh mát dễ chịu."
            };

            var bakeryComments = new string[] {
                "Bánh siêu mềm mịn, cốt bánh không bị khô.", "Lớp vỏ ngoài giòn rụm, nhân bên trong thơm béo.", "Bánh ngọt vừa phải, ăn kèm cà phê là chân ái.",
                "Bánh mới nướng nóng hổi, rất tươi ngon.", "Trời ơi món bánh này xịn thật sự, ăn một miếng là ghiền.", "Bánh được làm rất tỉ mỉ, mẫu mã cực kỳ đẹp mắt.",
                "Chất lượng bánh xuất sắc, bơ thơm lừng.", "Lớp kem béo béo ngậy ngậy không hề gắt cổ.", "Sáng nào cũng làm một chiếc này để bắt đầu ngày mới.",
                "Cốt bánh tơi xốp, giá trị hoàn toàn xứng đáng."
            };

            var merchComments = new string[] {
                "Chất lượng sản phẩm rất tốt, bao bì đẹp.", "Dùng để làm quà tặng cực kỳ sang trọng.", "Hàng đóng gói siêu cẩn thận, mở box mà thích mê.",
                "Nhìn bên ngoài còn đẹp hơn trong hình nữa.", "Xài rất bền, mẫu mã tối giản hợp gu mình.", "Cà phê thơm lức mũi luôn, lúc nào cũng mua sẵn ở nhà.",
                "Mua trải nghiệm thử ai dè ưng ý quá chừng."
            };

            var reviewsToInsert = new List<Review>();

            foreach (var p in products)
            {
                int reviewsCount = 0;
                string[] currentCommentSet;

                // Phân bổ tỷ trọng Đánh giá dựa trên thực tế môn quán Cà phê
                // Nước uống sẽ bán cực chạy, Bánh và Đồ lưu niệm thụ động hơn.
                if (p.Category.CategoryName.Contains("Cà phê truyền thống") || p.Category.CategoryName.Contains("Đá xay"))
                {
                    reviewsCount = random.Next(80, 150); // Món chủ đạo bán chạy nhất
                    currentCommentSet = drinkComments;
                }
                else if (p.Category.CategoryName.Contains("Trà") || p.Category.CategoryName.Contains("Sinh tố"))
                {
                    reviewsCount = random.Next(50, 90); // Món bán chạy nhì
                    currentCommentSet = drinkComments;
                }
                else if (p.Category.CategoryName.Contains("Bánh"))
                {
                    reviewsCount = random.Next(10, 30); // Bánh ăn kèm bán vừa
                    currentCommentSet = bakeryComments;
                }
                else
                {
                    reviewsCount = random.Next(5, 15); // Quà tặng, đóng gói ít mua hơn
                    currentCommentSet = merchComments;
                }

                for (int i = 0; i < reviewsCount; i++)
                {
                    // Tỉ lệ sao cho Cà phê cao hơn 
                    int rate = random.Next(1, 101); 
                    int rating = 5;
                    
                    if (p.Category.CategoryName.Contains("Cà phê")) {
                        if (rate <= 5) rating = 3; else if (rate <= 15) rating = 4; // Cà phê được rate 5 sao cực nhiều
                    } else {
                        if (rate <= 10) rating = 3; else if (rate <= 40) rating = 4; // Các món khác rate 5 sao bình thường
                    }
                    
                    var user = uList[random.Next(uList.Count)];
                    string comment = currentCommentSet[random.Next(currentCommentSet.Length)];

                    reviewsToInsert.Add(new Review
                    {
                        ProductId = p.ProductId,
                        UserId = user.UserId,
                        Rating = rating,
                        Comment = comment,
                        Status = 1,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(1, 24))
                    });
                }
            }

            if (reviewsToInsert.Any())
            {
                // Reset data cũ để apply data mới chuẩn tỷ trọng hơn
                context.Reviews.RemoveRange(context.Reviews);
                context.SaveChanges();
                
                context.Reviews.AddRange(reviewsToInsert);
                context.SaveChanges();
            }
        }

        public static void SeedVouchersData(ApplicationDbContext context)
        {
            if (context.Vouchers.Count() >= 11) return;

            // Thêm voucher đặc biệt tài khoản mới nếu chưa có
            if (!context.Vouchers.Any(v => v.Code == "NEWACCOUNT26"))
            {
                context.Vouchers.Add(new Voucher
                {
                    Code = "NEWACCOUNT26",
                    Name = "Chào mừng thành viên mới - Giảm 90%",
                    DiscountType = 1,          // % discount
                    DiscountValue = 90,         // 90%
                    MinOrderValue = 0,
                    UsageLimit = 999999,        // Không giới hạn tổng, ctrl theo UserVoucher
                    StartDate = DateTime.Now.AddYears(-1),
                    EndDate = DateTime.Now.AddYears(5),
                    Status = "Active"
                });
                context.SaveChanges();
            }

            if (context.Vouchers.Count() >= 11) return;

            var newVouchers = new List<Voucher>
            {
                new Voucher { Code = "WELCOME20", Name = "Ưu đãi thành viên mới - 20K", DiscountType = 2, DiscountValue = 20000, MinOrderValue = 100000, UsageLimit = 500, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(30), Status = "Active" },
                new Voucher { Code = "BB50K", Name = "Sale khủng giữa tháng - Giảm 50K", DiscountType = 2, DiscountValue = 50000, MinOrderValue = 250000, UsageLimit = 100, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(5), Status = "Active" },
                new Voucher { Code = "COFFEE10", Name = "Tri ân khách quen - Giảm 10%", DiscountType = 1, DiscountValue = 10, MinOrderValue = 150000, UsageLimit = 200, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(15), Status = "Active" },
                new Voucher { Code = "FREEDRINK", Name = "Voucher quy đổi đồ uống", DiscountType = 2, DiscountValue = 45000, MinOrderValue = 120000, UsageLimit = 50, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7), Status = "Active" },
                new Voucher { Code = "SWEETTREAT", Name = "Tặng kèm bánh ngọt - Trừ thẳng", DiscountType = 2, DiscountValue = 35000, MinOrderValue = 150000, UsageLimit = 150, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(20), Status = "Active" },
                new Voucher { Code = "BBAUTO15", Name = "Giảm giá chớp nhoáng 15%", DiscountType = 1, DiscountValue = 15, MinOrderValue = 300000, UsageLimit = 300, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(10), Status = "Active" },
                new Voucher { Code = "NIGHTOWL", Name = "Ưu đãi cú đêm - Phụ phí", DiscountType = 2, DiscountValue = 15000, MinOrderValue = 80000, UsageLimit = 1000, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1), Status = "Active" },
                new Voucher { Code = "WEEKEND25", Name = "Giảm giá đặc biệt cuối tuần 25K", DiscountType = 2, DiscountValue = 25000, MinOrderValue = 200000, UsageLimit = 250, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(3), Status = "Active" },
                new Voucher { Code = "BREADLOVE", Name = "Tín đồ Bánh mì - Giảm 20K", DiscountType = 2, DiscountValue = 20000, MinOrderValue = 90000, UsageLimit = 400, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(12), Status = "Active" },
                new Voucher { Code = "VIPONLY", Name = "Dành riêng khách hàng VIP 20%", DiscountType = 1, DiscountValue = 20, MinOrderValue = 500000, UsageLimit = 50, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(60), Status = "Active" }
            };

            context.Vouchers.AddRange(newVouchers);
            context.SaveChanges();
        }
    }
}
