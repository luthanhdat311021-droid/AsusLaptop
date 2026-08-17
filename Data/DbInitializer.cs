using Microsoft.EntityFrameworkCore;
using AsusLaptop.Helpers;
using AsusLaptop.Models;

namespace AsusLaptop.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            EnsureTablesCreated(context);
            if (context.Users.Any() || context.Products.Any()) return;

            // ===== USERS =====
            var admin = new User
            {
                Username = "admin",
                PasswordHash = PasswordHelper.HashPassword("admin123"),
                Email = "admin@asuslaptop.vn",
                FullName = "Administrator",
                Phone = "0987654321",
                Role = "Admin",
                CreatedAt = DateTime.Now.AddDays(-30)
            };
            var customer = new User
            {
                Username = "customer",
                PasswordHash = PasswordHelper.HashPassword("customer123"),
                Email = "customer@gmail.com",
                FullName = "Nguyễn Văn A",
                Phone = "0912345678",
                Role = "Customer",
                CreatedAt = DateTime.Now.AddDays(-15)
            };
            context.Users.AddRange(admin, customer);
            context.SaveChanges();

            // ===== PRODUCTS =====
            var products = new List<Product>
            {
                // ROG Series
                new Product
                {
                    Name = "ASUS ROG Strix G16 G614JV",
                    Price = 32990000, OriginalPrice = 37990000,
                    ImageUrl = "https://cdnv2.tgdd.vn/mwg-static/tgdd/Products/Images/44/333095/asus-rog-strix-g16-g614jv-i7-n3515w-1-638700327785084721-750x500.jpg",
                    Quantity = 25,
                    Brand = "ASUS", Series = "ROG Strix",
                    Description = "Laptop gaming flagship ROG Strix G16 với chip Intel Core i7 thế hệ 13, card đồ họa RTX 4060, màn hình 165Hz sắc nét. Thiết kế tản nhiệt Tri-Fan Technology đột phá, bàn phím RGB per-key, pin 90WHr cho trải nghiệm gaming bất tận.",
                    ScreenSize = "16 inch", ScreenResolution = "FHD 165Hz",
                    CPU = "Intel Core i7-13650HX", RAM = "16 GB DDR5",
                    Storage = "512 GB NVMe SSD", GPU = "NVIDIA GeForce RTX 4060 8GB",
                    Battery = "90 WHr", Weight = "2.5 kg", OS = "Windows 11 Home"
                },
                new Product
                {
                    Name = "ASUS ROG Zephyrus G14 GA403UV",
                    Price = 45990000, OriginalPrice = 52000000,
                    ImageUrl = "https://m.media-amazon.com/images/I/61SEuRLdrYL._AC_UF894,1000_QL80_.jpg",
                    Quantity = 15,
                    Brand = "ASUS", Series = "ROG Zephyrus",
                    Description = "ROG Zephyrus G14 - siêu phẩm gaming mỏng nhẹ hàng đầu. AMD Ryzen 9 mạnh mẽ kết hợp RTX 4060, màn OLED 120Hz rực rỡ. Thiết kế AniMe Matrix LED nắp máy cực kỳ ấn tượng, trọng lượng chỉ 1.65 kg.",
                    ScreenSize = "14 inch", ScreenResolution = "2.8K OLED 120Hz",
                    CPU = "AMD Ryzen 9 8945HS", RAM = "32 GB LPDDR5X",
                    Storage = "1 TB NVMe SSD", GPU = "NVIDIA GeForce RTX 4060 8GB",
                    Battery = "73 WHr", Weight = "1.65 kg", OS = "Windows 11 Home"
                },
                new Product
                {
                    Name = "ASUS ROG Flow X13 GV302XV",
                    Price = 52990000, OriginalPrice = 60000000,
                    ImageUrl = "https://i.rtings.com/assets/products/XiiQ8wbP/asus-rog-flow-x13-2023/design-medium.jpg?format=auto",
                    Quantity = 10,
                    Brand = "ASUS", Series = "ROG Flow",
                    Description = "ROG Flow X13 - laptop gaming 2-in-1 siêu độc đáo. Xoay 360° linh hoạt, RTX 4070 trong thân máy siêu mỏng, màn hình 165Hz cảm ứng. Hoàn hảo cho cả gaming lẫn sáng tạo nội dung.",
                    ScreenSize = "13.4 inch", ScreenResolution = "QHD+ 165Hz Touch",
                    CPU = "AMD Ryzen 9 7940HS", RAM = "16 GB LPDDR5",
                    Storage = "512 GB NVMe SSD", GPU = "NVIDIA GeForce RTX 4070 8GB",
                    Battery = "75 WHr", Weight = "1.38 kg", OS = "Windows 11 Home"
                },
                // TUF Series
                new Product
                {
                    Name = "ASUS TUF Gaming F15 FX507VV",
                    Price = 22990000, OriginalPrice = 26990000,
                    ImageUrl = "https://vn.store.asus.com/media/catalog/product/cache/2a6b0744b87cbe1990f7a65c1fd3659e/a/s/asus_tuf_gaming_f15_2023__fx507vv-lp304w_1_.jpg",
                    Quantity = 40,
                    Brand = "ASUS", Series = "TUF Gaming",
                    Description = "TUF Gaming F15 - lựa chọn gaming tối ưu cho sinh viên. Intel Core i7 thế hệ 13 mạnh mẽ, RTX 4060 chơi được mọi game AAA, thiết kế cứng cáp đạt chuẩn quân đội MIL-STD-810H, pin lâu 90WHr.",
                    ScreenSize = "15.6 inch", ScreenResolution = "FHD 144Hz",
                    CPU = "Intel Core i7-13620H", RAM = "16 GB DDR4",
                    Storage = "512 GB NVMe SSD", GPU = "NVIDIA GeForce RTX 4060 8GB",
                    Battery = "90 WHr", Weight = "2.2 kg", OS = "Windows 11 Home"
                },
                new Product
                {
                    Name = "ASUS TUF Gaming A15 FA507NV",
                    Price = 18990000, OriginalPrice = 21990000,
                    ImageUrl = "https://vn.store.asus.com/media/catalog/product/cache/2a6b0744b87cbe1990f7a65c1fd3659e/a/s/asus_tuf_gaming_a15_2023__fa507nv-lp046w_1.png",
                    Quantity = 50,
                    Brand = "ASUS", Series = "TUF Gaming",
                    Description = "TUF Gaming A15 mang sức mạnh AMD Ryzen 7 thế hệ 7000 kết hợp RTX 4060. Màn hình FHD 144Hz mượt mà, bàn phím phủ tán nhiệt, bền bỉ chuẩn MIL-STD-810H đối mặt mọi điều kiện khắc nghiệt.",
                    ScreenSize = "15.6 inch", ScreenResolution = "FHD 144Hz",
                    CPU = "AMD Ryzen 7 7745HX", RAM = "16 GB DDR5",
                    Storage = "512 GB NVMe SSD", GPU = "NVIDIA GeForce RTX 4060 8GB",
                    Battery = "90 WHr", Weight = "2.1 kg", OS = "Windows 11 Home"
                },
                // ZenBook Series
                new Product
                {
                    Name = "ASUS ZenBook 14 UX3405MA",
                    Price = 28990000, OriginalPrice = 33000000,
                    ImageUrl = "https://cdn.ankhang.vn/media/product/24308_thumb_laptop_asus_zenbook_14_oled_ux3405ma_pp151w.jpg",
                    Quantity = 30,
                    Brand = "ASUS", Series = "ZenBook",
                    Description = "ZenBook 14 OLED - đỉnh cao laptop mỏng nhẹ văn phòng. Màn OLED 2.8K 120Hz cực đẹp, Intel Core Ultra 9 AI mạnh mẽ, thân máy chỉ 1.2 kg siêu cơ động. Tích hợp Copilot+ PC với NPU 40 TOPS.",
                    ScreenSize = "14 inch", ScreenResolution = "2.8K OLED 120Hz",
                    CPU = "Intel Core Ultra 9 185H", RAM = "32 GB LPDDR5X",
                    Storage = "1 TB NVMe SSD", GPU = "Intel Arc Graphics",
                    Battery = "75 WHr", Weight = "1.2 kg", OS = "Windows 11 Pro"
                },
                new Product
                {
                    Name = "ASUS ZenBook S13 OLED UX5304VA",
                    Price = 35990000, OriginalPrice = 42000000,
                    ImageUrl = "https://product.hstatic.net/200000722513/product/nq126w_019cd3fd4ad14bf2a3f8b52010a318bf_ee222e72e1744d838a3cad3abbb77893_grande.png",
                    Quantity = 20,
                    Brand = "ASUS", Series = "ZenBook",
                    Description = "ZenBook S13 OLED - siêu phẩm mỏng nhẹ nhất thế giới. Màn OLED 2.8K 60Hz rực rỡ, Intel Core i7 thế hệ 13, thân nhôm CNC siêu sang trọng chỉ 10.9mm. Nhận Gartner Cool Vendor 2023.",
                    ScreenSize = "13.3 inch", ScreenResolution = "2.8K OLED 60Hz",
                    CPU = "Intel Core i7-1355U", RAM = "16 GB LPDDR5",
                    Storage = "512 GB NVMe SSD", GPU = "Intel Iris Xe Graphics",
                    Battery = "63 WHr", Weight = "1.0 kg", OS = "Windows 11 Pro"
                },
                // VivoBook Series
                new Product
                {
                    Name = "ASUS VivoBook 15 OLED A1505VA",
                    Price = 16990000, OriginalPrice = 19990000,
                    ImageUrl = "https://vn.store.asus.com/media/catalog/product/cache/2a6b0744b87cbe1990f7a65c1fd3659e/a/1/a1505va-l1688w.jpg",
                    Quantity = 60,
                    Brand = "ASUS", Series = "VivoBook",
                    Description = "VivoBook 15 OLED - lựa chọn tuyệt vời cho học sinh sinh viên. Màn hình OLED 60Hz sắc màu rực rỡ, Intel Core i5 thế hệ 13 đủ mạnh cho mọi tác vụ học tập, giải trí, thiết kế cơ bản. Giá tốt nhất phân khúc.",
                    ScreenSize = "15.6 inch", ScreenResolution = "FHD OLED 60Hz",
                    CPU = "Intel Core i5-13500H", RAM = "8 GB DDR4",
                    Storage = "512 GB NVMe SSD", GPU = "Intel Iris Xe Graphics",
                    Battery = "50 WHr", Weight = "1.65 kg", OS = "Windows 11 Home"
                },
                new Product
                {
                    Name = "ASUS VivoBook 16X K3605ZF",
                    Price = 14990000, OriginalPrice = 17990000,
                    ImageUrl = "https://phucanhcdn.com/media/product/55641_55641_asus_gaming_vivobook_k3605zf_rp634w.jpeg",
                    Quantity = 70,
                    Brand = "ASUS", Series = "VivoBook",
                    Description = "VivoBook 16X - màn hình lớn 16 inch FHD, Intel Core i5 thế hệ 12, card RTX 2050 chơi game nhẹ cực tốt. Thiết kế gọn nhẹ 1.88 kg, bàn phím đèn nền, lý tưởng cho học tập và làm việc linh hoạt.",
                    ScreenSize = "16 inch", ScreenResolution = "FHD 60Hz",
                    CPU = "Intel Core i5-12500H", RAM = "8 GB DDR4",
                    Storage = "512 GB NVMe SSD", GPU = "NVIDIA GeForce RTX 2050 4GB",
                    Battery = "50 WHr", Weight = "1.88 kg", OS = "Windows 11 Home"
                },
                // ProArt Series
                new Product
                {
                    Name = "ASUS ProArt Studiobook 16 H7600ZX",
                    Price = 79990000, OriginalPrice = 90000000,
                    ImageUrl = "https://vn.store.asus.com/media/catalog/product/cache/6517c62f5899ad6aa0ba23ceb3eeff97/v/i/viber_image_2023-12-15_16-01-02-964.jpg",
                    Quantity = 8,
                    Brand = "ASUS", Series = "ProArt",
                    Description = "ProArt Studiobook 16 - vũ khí tối thượng cho nhà sáng tạo chuyên nghiệp. Màn OLED 4K 120Hz đạt 100% DCI-P3, RTX 3080 Ti mạnh nhất laptop, ASUS Dial vật lý độc quyền điều chỉnh chính xác. Được chứng nhận PANTONE Validated.",
                    ScreenSize = "16 inch", ScreenResolution = "4K OLED 120Hz",
                    CPU = "Intel Core i9-12900H", RAM = "64 GB DDR5",
                    Storage = "2 TB NVMe SSD", GPU = "NVIDIA GeForce RTX 3080 Ti 16GB",
                    Battery = "90 WHr", Weight = "2.4 kg", OS = "Windows 11 Pro"
                },
                // ExpertBook Series
                new Product
                {
                    Name = "ASUS ExpertBook B9 OLED B9403CVA",
                    Price = 42990000, OriginalPrice = 48000000,
                    ImageUrl = "https://cdn.ankhang.vn/media/product/25336_asus_expertbook_b9_oled_b9403cva_km0157x_.jpg",
                    Quantity = 12,
                    Brand = "ASUS", Series = "ExpertBook",
                    Description = "ExpertBook B9 OLED - laptop doanh nhân cao cấp nhất. Nhẹ nhất thế giới 870g, màn OLED 2.8K siêu đẹp, bảo mật vân tay + IR khuôn mặt + TPM 2.0, pin khủng 63WHr dùng được 15 tiếng. Đạt 12 chứng nhận MIL-STD.",
                    ScreenSize = "14 inch", ScreenResolution = "2.8K OLED 60Hz",
                    CPU = "Intel Core i7-1355U", RAM = "32 GB LPDDR5",
                    Storage = "1 TB NVMe SSD", GPU = "Intel Iris Xe Graphics",
                    Battery = "63 WHr", Weight = "0.87 kg", OS = "Windows 11 Pro"
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            // ===== SEED ORDERS =====
            var p1 = products[0]; // ROG Strix G16
            var p4 = products[3]; // TUF F15
            var p8 = products[7]; // VivoBook 15

            var order1 = new Order
            {
                UserId = customer.Id,
                CustomerName = "Nguyễn Văn A",
                Phone = "0912345678",
                Address = "123 Nguyễn Huệ, Q.1, TP.HCM",
                Email = "customer@gmail.com",
                OrderDate = DateTime.Now.AddDays(-7),
                TotalAmount = p4.Price,
                Status = "Completed"
            };
            var order2 = new Order
            {
                CustomerName = "Trần Thị B",
                Phone = "0933221144",
                Address = "456 Lê Lợi, Q.3, TP.HCM",
                Email = "tranb@gmail.com",
                OrderDate = DateTime.Now.AddDays(-3),
                TotalAmount = p1.Price,
                Status = "Completed"
            };
            var order3 = new Order
            {
                CustomerName = "Lê Văn C",
                Phone = "0966778899",
                Address = "789 CMT8, Tân Bình, TP.HCM",
                Email = "levanc@gmail.com",
                OrderDate = DateTime.Now.AddDays(-1),
                TotalAmount = p8.Price * 2,
                Status = "Processing"
            };
            var order4 = new Order
            {
                CustomerName = "Phạm Thị D",
                Phone = "0911223344",
                Address = "321 Điện Biên Phủ, Bình Thạnh, TP.HCM",
                Email = "phamd@gmail.com",
                OrderDate = DateTime.Now,
                TotalAmount = p1.Price,
                Status = "Pending"
            };

            context.Orders.AddRange(order1, order2, order3, order4);
            context.SaveChanges();

            context.OrderDetails.AddRange(
                new OrderDetail { OrderId = order1.Id, ProductId = p4.Id, Quantity = 1, Price = p4.Price },
                new OrderDetail { OrderId = order2.Id, ProductId = p1.Id, Quantity = 1, Price = p1.Price },
                new OrderDetail { OrderId = order3.Id, ProductId = p8.Id, Quantity = 2, Price = p8.Price },
                new OrderDetail { OrderId = order4.Id, ProductId = p1.Id, Quantity = 1, Price = p1.Price }
            );
            context.SaveChanges();
        }


        // =====================================================
        // Gọi sau Initialize() để seed biến thể + serial
        // =====================================================
        public static void SeedVariants(ApplicationDbContext context)
        {
            if (context.ProductVariants.Any()) return; // đã seed rồi thì bỏ qua

            var variantDefs = new List<(int pid, string ram, string color, string hex, decimal adj, int stock, bool def)>
        {
            // ROG Strix G16
            (1,"16 GB DDR5","Eclipse Gray","#3D3D3D",0,8,true),
            (1,"16 GB DDR5","Volt Green","#7CB518",0,6,false),
            (1,"32 GB DDR5","Eclipse Gray","#3D3D3D",3000000,5,false),
            (1,"32 GB DDR5","Volt Green","#7CB518",3000000,4,false),
            // ROG Zephyrus G14
            (2,"16 GB LPDDR5X","Eclipse Gray","#3D3D3D",-5000000,4,false),
            (2,"32 GB LPDDR5X","Eclipse Gray","#3D3D3D",0,5,true),
            (2,"32 GB LPDDR5X","Platinum White","#F0F0F0",0,3,false),
            (2,"32 GB LPDDR5X","Nebula Green","#4CAF50",0,3,false),
            // ROG Flow X13
            (3,"16 GB LPDDR5","Inkwell Black","#1A1A2E",0,6,true),
            (3,"16 GB LPDDR5","Luna White","#F5F5F5",0,5,false),
            (3,"32 GB LPDDR5","Inkwell Black","#1A1A2E",5000000,4,false),
            (3,"32 GB LPDDR5","Luna White","#F5F5F5",5000000,3,false),
            // TUF F15
            (4,"16 GB DDR4","Mecha Gray","#6B7280",0,15,true),
            (4,"16 GB DDR4","Jaeger Gray","#4B5563",0,12,false),
            (4,"32 GB DDR4","Mecha Gray","#6B7280",2000000,8,false),
            (4,"32 GB DDR4","Jaeger Gray","#4B5563",2000000,5,false),
            // TUF A15
            (5,"16 GB DDR5","Mecha Gray","#6B7280",0,18,true),
            (5,"16 GB DDR5","Off Black","#2D2D2D",0,15,false),
            (5,"32 GB DDR5","Mecha Gray","#6B7280",2000000,10,false),
            (5,"32 GB DDR5","Off Black","#2D2D2D",2000000,7,false),
            // ZenBook 14
            (6,"16 GB LPDDR5X","Ponder Blue","#4A7C9E",-3000000,5,false),
            (6,"32 GB LPDDR5X","Ponder Blue","#4A7C9E",0,7,true),
            (6,"32 GB LPDDR5X","Jasper Slate","#8B7355",0,5,false),
            (6,"32 GB LPDDR5X","Foggy Silver","#C0C0C0",0,4,false),
            // ZenBook S13
            (7,"16 GB LPDDR5","Basalt Gray","#5C5C5C",0,8,true),
            (7,"16 GB LPDDR5","Refined White","#EFEFEF",0,7,false),
            (7,"32 GB LPDDR5","Basalt Gray","#5C5C5C",3000000,4,false),
            (7,"32 GB LPDDR5","Refined White","#EFEFEF",3000000,3,false),
            // VivoBook 15
            (8,"8 GB DDR4","Midnight Black","#0D0D0D",0,12,true),
            (8,"8 GB DDR4","Quiet Blue","#5C7FA3",0,10,false),
            (8,"16 GB DDR4","Midnight Black","#0D0D0D",2000000,6,false),
            (8,"16 GB DDR4","Quiet Blue","#5C7FA3",2000000,4,false),
            // VivoBook 16X
            (9,"8 GB DDR4","Indie Black","#1C1C1C",0,15,true),
            (9,"8 GB DDR4","Cool Silver","#A8A8A8",0,12,false),
            (9,"16 GB DDR4","Indie Black","#1C1C1C",1500000,8,false),
            (9,"16 GB DDR4","Cool Silver","#A8A8A8",1500000,6,false),
            // ProArt
            (10,"32 GB DDR5","Nano Black","#1A1A1A",-10000000,3,false),
            (10,"64 GB DDR5","Nano Black","#1A1A1A",0,4,true),
            (10,"64 GB DDR5","Star Gray","#707070",0,3,false),
            (10,"96 GB DDR5","Nano Black","#1A1A1A",10000000,2,false),
            // ExpertBook
            (11,"16 GB LPDDR5","Star Black","#0A0A0A",-3000000,4,false),
            (11,"32 GB LPDDR5","Star Black","#0A0A0A",0,5,true),
            (11,"32 GB LPDDR5","Pure White","#F8F8F8",0,3,false),
        };

            var products = context.Products.ToList();
            var nextSeqCache = new Dictionary<string, int>(); // prefix-pattern -> số tiếp theo còn trống

            foreach (var (pid, ram, color, hex, adj, stock, isDefault) in variantDefs)
            {
                var v = new AsusLaptop.Models.ProductVariant
                {
                    ProductId = pid,
                    RAM = ram,
                    Color = color,
                    ColorHex = hex,
                    PriceAdjust = adj,
                    Stock = stock,
                    IsDefault = isDefault,
                    CreatedAt = DateTime.Now
                };
                context.ProductVariants.Add(v);
                context.SaveChanges();

                var series = products.FirstOrDefault(p => p.Id == pid)?.Series ?? "ASU";
                var pattern = AsusLaptop.Models.SerialNumberGenerator.BuildPrefixPattern(series);
                if (!nextSeqCache.TryGetValue(pattern, out int startSeq))
                {
                    var existingMax = context.SerialNumbers
                        .Where(s => s.SerialNo.StartsWith(pattern))
                        .Select(s => s.SerialNo)
                        .ToList()
                        .Select(s => int.TryParse(s.Length > pattern.Length ? s[pattern.Length..] : "", out int n) ? n : 0)
                        .DefaultIfEmpty(0)
                        .Max();
                    startSeq = existingMax + 1;
                }

                var batchCount = stock > 5 ? 5 : stock;
                var serials = AsusLaptop.Models.SerialNumberGenerator.GenerateBatch(series, batchCount, startSeq);
                nextSeqCache[pattern] = startSeq + batchCount;

                foreach (var sn in serials)
                {
                    context.SerialNumbers.Add(new AsusLaptop.Models.SerialNumber
                    {
                        SerialNo = sn,
                        ProductId = pid,
                        VariantId = v.Id,
                        Status = "Available",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                context.SaveChanges();
            }
        }

        public static void EnsureTablesCreated(ApplicationDbContext context)
        {
            try
            {
                string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductRegistrations')
                    BEGIN
                        CREATE TABLE [ProductRegistrations] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] INT NULL,
                            [SerialNo] NVARCHAR(30) NOT NULL,
                            [ProductId] INT NOT NULL,
                            [FullName] NVARCHAR(100) NOT NULL,
                            [Phone] NVARCHAR(20) NOT NULL,
                            [Email] NVARCHAR(100) NULL,
                            [PurchaseDate] DATETIME2 NOT NULL,
                            [PurchasePlace] NVARCHAR(200) NULL,
                            [RegisteredAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
                            [Status] NVARCHAR(20) NOT NULL DEFAULT 'Approved',
                            [Note] NVARCHAR(MAX) NULL
                        );
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaintenanceBookings')
                    BEGIN
                        CREATE TABLE [MaintenanceBookings] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] INT NULL,
                            [SerialNo] NVARCHAR(50) NOT NULL,
                            [ProductName] NVARCHAR(150) NOT NULL,
                            [ServiceType] NVARCHAR(100) NOT NULL,
                            [ServiceMethod] NVARCHAR(50) NOT NULL,
                            [PreferredDate] DATETIME2 NOT NULL,
                            [PreferredTime] NVARCHAR(50) NOT NULL,
                            [CustomerName] NVARCHAR(100) NOT NULL,
                            [Phone] NVARCHAR(20) NOT NULL,
                            [Address] NVARCHAR(250) NULL,
                            [Note] NVARCHAR(500) NULL,
                            [Status] NVARCHAR(30) NOT NULL DEFAULT 'Pending',
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE()
                        );
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
                    BEGIN
                        CREATE TABLE [Notifications] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] INT NOT NULL,
                            [Title] NVARCHAR(200) NOT NULL,
                            [Message] NVARCHAR(1000) NOT NULL,
                            [Type] NVARCHAR(50) NOT NULL DEFAULT 'System',
                            [ActionUrl] NVARCHAR(500) NULL,
                            [IsRead] BIT NOT NULL DEFAULT 0,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE()
                        );
                    END;
                ";
                context.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Soft fallback for SQL execution permissions
            }
        }
    }
}
