/* =====================================================================================
   ASUSLAPTOP - SCRIPT TỔNG HỢP TOÀN BỘ DATABASE (gộp từ 7 file gốc)
   ---------------------------------------------------------------------------------
   Gồm nội dung của:
     1. create_database_sqlserver.sql
     2. Migration_AddVariants.sql
     3. Migration_AddNewTables (1).sql
     4. AddNewFeatures_SQLServer.sql
     5. AddReturnRefund_Migration.sql
     6. AddViewCount_Migration.sql          (đã gộp thẳng vào cột Products.ViewCount)
     7. Migration_SyncProductQuantity.sql   (đặt cuối cùng, chạy an toàn nhiều lần)

   Khác với các file gốc, các bảng ở đây được sắp xếp lại theo đúng THỨ TỰ PHỤ THUỘC
   (bảng bị tham chiếu luôn được tạo trước), nên toàn bộ khóa ngoại được khai báo
   NGAY TRONG CREATE TABLE — không cần ALTER TABLE ADD CONSTRAINT về sau, và không
   còn lỗi "Invalid object name" do chạy sai thứ tự như 7 file rời rạc trước đây.

   Cách dùng: SSMS -> kết nối SQL Server -> New Query -> dán toàn bộ file này -> Execute (F5)
   ===================================================================================== */

USE master;
GO
ALTER DATABASE AsusLaptopDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'AsusLaptopDB')
    DROP DATABASE AsusLaptopDB;
GO

CREATE DATABASE AsusLaptopDB;
GO

USE AsusLaptopDB;
GO

-- ════════════════════════════════════════════════════════════════
-- 1. USERS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)   NOT NULL,
    PasswordHash NVARCHAR(MAX)  NOT NULL,
    Email        NVARCHAR(100)  NOT NULL,
    FullName     NVARCHAR(100)  NULL,
    Phone        NVARCHAR(20)   NULL,
    Role         NVARCHAR(20)   NOT NULL DEFAULT 'Customer',
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE(),
    GoogleId     NVARCHAR(MAX)  NULL,
    AvatarUrl    NVARCHAR(MAX)  NULL,
    FacebookId   NVARCHAR(MAX)  NULL,
    FaceToken    NVARCHAR(MAX)  NULL
);
GO

-- ════════════════════════════════════════════════════════════════
-- 2. CATEGORIES (danh mục sản phẩm, hỗ trợ cha-con)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Categories (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NULL,
    ParentId    INT            NULL,
    ImageUrl    NVARCHAR(300)  NULL,
    SortOrder   INT            NOT NULL CONSTRAINT DF_Categories_SortOrder DEFAULT (0),
    IsActive    BIT            NOT NULL CONSTRAINT DF_Categories_IsActive  DEFAULT (1),
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION
);
GO

-- ════════════════════════════════════════════════════════════════
-- 3. BRANDS (thương hiệu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Brands (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NULL,
    LogoUrl     NVARCHAR(300)  NULL,
    WebsiteUrl  NVARCHAR(300)  NULL,
    IsActive    BIT            NOT NULL CONSTRAINT DF_Brands_IsActive  DEFAULT (1),
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Brands_CreatedAt DEFAULT (SYSDATETIME())
);
GO

-- ════════════════════════════════════════════════════════════════
-- 4. PRODUCTS  (đã gồm CategoryId, BrandId, ViewCount, VideoUrl ngay từ đầu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Products (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Name             NVARCHAR(150)  NOT NULL,
    CategoryId       INT            NULL,
    BrandId          INT            NULL,
    Price            DECIMAL(18,2)  NOT NULL,
    OriginalPrice    DECIMAL(18,2)  NOT NULL DEFAULT 0,
    ImageUrl         NVARCHAR(500)  NOT NULL DEFAULT '',
    Quantity         INT            NOT NULL DEFAULT 0,
    Description      NVARCHAR(MAX)  NOT NULL DEFAULT '',
    Brand            NVARCHAR(50)   NOT NULL DEFAULT '',
    Series           NVARCHAR(50)   NOT NULL DEFAULT '',
    ScreenSize       NVARCHAR(50)   NOT NULL DEFAULT '',
    ScreenResolution NVARCHAR(50)   NOT NULL DEFAULT '',
    CPU              NVARCHAR(80)   NOT NULL DEFAULT '',
    RAM              NVARCHAR(20)   NOT NULL DEFAULT '',
    Storage          NVARCHAR(30)   NOT NULL DEFAULT '',
    GPU              NVARCHAR(80)   NOT NULL DEFAULT '',
    Battery          NVARCHAR(30)   NOT NULL DEFAULT '',
    Weight           NVARCHAR(30)   NOT NULL DEFAULT '',
    OS               NVARCHAR(30)   NOT NULL DEFAULT '',
    CreatedAt        DATETIME2      NOT NULL DEFAULT GETDATE(),
    ViewCount        INT            NOT NULL DEFAULT 0,
    VideoUrl         NVARCHAR(500)  NULL,

    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Products_Brands     FOREIGN KEY (BrandId)     REFERENCES dbo.Brands(Id)     ON DELETE SET NULL
);
GO

-- ════════════════════════════════════════════════════════════════
-- 5. PRODUCT VARIANTS (biến thể RAM/màu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ProductVariants (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProductId   INT NOT NULL,
    RAM         NVARCHAR(50) NOT NULL,
    Color       NVARCHAR(50) NOT NULL,
    ColorHex    NVARCHAR(10) NOT NULL DEFAULT '#333333',
    PriceAdjust DECIMAL(18,2) NOT NULL DEFAULT 0,
    Stock       INT NOT NULL DEFAULT 0,
    IsDefault   BIT NOT NULL DEFAULT 0,
    CreatedAt   DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_PV_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_PV_Product_RAM_Color UNIQUE (ProductId, RAM, Color)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 6. SERIAL NUMBERS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.SerialNumbers (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    SerialNo      NVARCHAR(30) NOT NULL UNIQUE,
    ProductId     INT NOT NULL,
    VariantId     INT NULL,
    Status        NVARCHAR(20) NOT NULL DEFAULT 'Available',
    OrderDetailId INT NULL,   -- FK thêm bên dưới, sau khi OrderDetails được tạo
    WarrantyEnd   DATE NULL,
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt     DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_SN_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SN_Variants FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id),
    CONSTRAINT CK_SN_Status CHECK (Status IN ('Available','Reserved','Sold','Warranty'))
);
GO

-- ════════════════════════════════════════════════════════════════
-- 7. ORDERS (đã gồm Voucher, Shipper/toạ độ ngay từ đầu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Orders (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT            NULL,
    CustomerName  NVARCHAR(100)  NOT NULL,
    Phone         NVARCHAR(20)   NOT NULL,
    Address       NVARCHAR(250)  NOT NULL,
    Email         NVARCHAR(100)  NOT NULL DEFAULT '',
    TotalAmount   DECIMAL(18,2)  NOT NULL,
    OrderDate     DATETIME2      NOT NULL DEFAULT GETDATE(),
    Status        NVARCHAR(50)   NOT NULL DEFAULT 'Pending',
    Note          NVARCHAR(MAX)  NULL,
    PaymentMethod NVARCHAR(30)   NOT NULL DEFAULT 'COD',
    PaymentStatus NVARCHAR(30)   NOT NULL DEFAULT 'Unpaid',
    VoucherCode        NVARCHAR(30)   NULL,
    DiscountAmount     DECIMAL(18,2)  NOT NULL DEFAULT 0,
    ShipperName        NVARCHAR(100)  NULL,
    ShipperPhone       NVARCHAR(20)   NULL,
    ShipperLat         FLOAT          NULL,
    ShipperLng         FLOAT          NULL,
    DestinationLat     FLOAT          NULL,
    DestinationLng     FLOAT          NULL,
    LastLocationUpdate DATETIME2      NULL,

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 8. ORDER DETAILS (đã gồm VariantId ngay từ đầu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.OrderDetails (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    OrderId   INT           NOT NULL,
    ProductId INT           NOT NULL,
    VariantId INT           NULL,
    Quantity  INT           NOT NULL,
    Price     DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders   FOREIGN KEY (OrderId)   REFERENCES dbo.Orders(Id),
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_OD_Variants           FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id)
);
GO

-- Bổ sung FK từ SerialNumbers -> OrderDetails (đến giờ OrderDetails đã tồn tại)
ALTER TABLE dbo.SerialNumbers ADD CONSTRAINT FK_SN_OrderDetails
    FOREIGN KEY (OrderDetailId) REFERENCES dbo.OrderDetails(Id);
GO

-- ════════════════════════════════════════════════════════════════
-- 9. CART ITEMS (đã gồm VariantId ngay từ đầu)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.CartItems (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    SessionId NVARCHAR(100) NOT NULL,
    ProductId INT           NOT NULL,
    VariantId INT           NULL,
    Quantity  INT           NOT NULL,

    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_Cart_Variants      FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id) ON DELETE SET NULL
);
GO

-- ════════════════════════════════════════════════════════════════
-- 10. PRODUCT IMAGES
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ProductImages (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT           NOT NULL,
    ImageUrl  NVARCHAR(500) NOT NULL,
    AltText   NVARCHAR(200) NULL,
    SortOrder INT           NOT NULL CONSTRAINT DF_ProductImages_SortOrder DEFAULT (0),
    IsPrimary BIT           NOT NULL CONSTRAINT DF_ProductImages_IsPrimary DEFAULT (0),
    VariantId INT           NULL,
    CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_ProductImages_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductImages_Variants FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id) ON DELETE NO ACTION
);
GO

-- ════════════════════════════════════════════════════════════════
-- 11. PRODUCT SPECIFICATIONS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ProductSpecifications (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT           NOT NULL,
    SpecName  NVARCHAR(100) NOT NULL,
    SpecValue NVARCHAR(500) NOT NULL,
    GroupName NVARCHAR(100) NULL,
    SortOrder INT           NOT NULL CONSTRAINT DF_ProductSpecs_SortOrder DEFAULT (0),

    CONSTRAINT FK_ProductSpecs_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);
GO

-- ════════════════════════════════════════════════════════════════
-- 12. NOTIFICATIONS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Notifications (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT            NULL,
    Title     NVARCHAR(200)  NOT NULL,
    Message   NVARCHAR(MAX)  NOT NULL,
    Type      NVARCHAR(30)   NOT NULL CONSTRAINT DF_Notifications_Type    DEFAULT ('System'),
    IsRead    BIT            NOT NULL CONSTRAINT DF_Notifications_IsRead  DEFAULT (0),
    ActionUrl NVARCHAR(300)  NULL,
    CreatedAt DATETIME2      NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSDATETIME()),
    ReadAt    DATETIME2      NULL,

    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Notifications_Type CHECK (Type IN ('Order','Promotion','System','Review','Stock'))
);
CREATE INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications (UserId, IsRead) INCLUDE (CreatedAt);
GO

-- ════════════════════════════════════════════════════════════════
-- 13. CHAT HISTORIES
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ChatHistories (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT            NULL,
    SessionId   NVARCHAR(100)  NOT NULL,
    SenderRole  NVARCHAR(20)   NOT NULL CONSTRAINT DF_ChatHistories_SenderRole  DEFAULT ('User'),
    Message     NVARCHAR(MAX)  NOT NULL,
    MessageType NVARCHAR(30)   NOT NULL CONSTRAINT DF_ChatHistories_MessageType DEFAULT ('Text'),
    SentAt      DATETIME2      NOT NULL CONSTRAINT DF_ChatHistories_SentAt      DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_ChatHistories_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
    CONSTRAINT CK_ChatHistories_SenderRole CHECK (SenderRole IN ('User','Bot','Admin'))
);
CREATE INDEX IX_ChatHistories_SessionId ON dbo.ChatHistories (SessionId) INCLUDE (SentAt);
GO

-- ════════════════════════════════════════════════════════════════
-- 14. PAYMENT TRANSACTIONS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.PaymentTransactions (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    OrderId         INT            NOT NULL,
    Gateway         NVARCHAR(30)   NOT NULL,
    TransactionCode NVARCHAR(100)  NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    Currency        NVARCHAR(10)   NOT NULL CONSTRAINT DF_PayTrans_Currency  DEFAULT ('VND'),
    Status          NVARCHAR(20)   NOT NULL CONSTRAINT DF_PayTrans_Status    DEFAULT ('Pending'),
    RawResponse     NVARCHAR(MAX)  NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_PayTrans_CreatedAt DEFAULT (SYSDATETIME()),
    CompletedAt     DATETIME2      NULL,

    CONSTRAINT FK_PaymentTransactions_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_PaymentTransactions_TransactionCode UNIQUE (TransactionCode),
    CONSTRAINT CK_PaymentTransactions_Status CHECK (Status IN ('Pending','Success','Failed','Refunded'))
);
GO

-- ════════════════════════════════════════════════════════════════
-- 15. USER ADDRESSES
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.UserAddresses (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT            NOT NULL,
    RecipientName NVARCHAR(100)  NOT NULL,
    Phone         NVARCHAR(20)   NOT NULL,
    AddressLine   NVARCHAR(300)  NOT NULL,
    Ward          NVARCHAR(100)  NULL,
    District      NVARCHAR(100)  NULL,
    City          NVARCHAR(100)  NOT NULL,
    IsDefault     BIT            NOT NULL CONSTRAINT DF_UserAddresses_IsDefault DEFAULT (0),
    Label         NVARCHAR(50)   NULL,
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_UserAddresses_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_UserAddresses_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

-- ════════════════════════════════════════════════════════════════
-- 16. INVENTORY LOGS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.InventoryLogs (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    ProductId       INT            NOT NULL,
    VariantId       INT            NULL,
    QuantityChange  INT            NOT NULL,
    StockAfter      INT            NOT NULL,
    Reason          NVARCHAR(30)   NOT NULL CONSTRAINT DF_InventoryLogs_Reason    DEFAULT ('Adjustment'),
    Note            NVARCHAR(500)  NULL,
    CreatedByUserId INT            NULL,
    OrderId         INT            NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_InventoryLogs_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_InventoryLogs_Products FOREIGN KEY (ProductId)       REFERENCES dbo.Products(Id)        ON DELETE CASCADE,
    CONSTRAINT FK_InventoryLogs_Variants FOREIGN KEY (VariantId)       REFERENCES dbo.ProductVariants(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_InventoryLogs_Users    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)           ON DELETE SET NULL,
    CONSTRAINT FK_InventoryLogs_Orders   FOREIGN KEY (OrderId)         REFERENCES dbo.Orders(Id)          ON DELETE NO ACTION,
    CONSTRAINT CK_InventoryLogs_Reason CHECK (Reason IN ('Import','Sale','Return','Adjustment','Damage'))
);
CREATE INDEX IX_InventoryLogs_Product_Variant ON dbo.InventoryLogs (ProductId, VariantId) INCLUDE (CreatedAt, QuantityChange, StockAfter);
GO

-- ════════════════════════════════════════════════════════════════
-- 17. REVIEWS (đánh giá 5 sao)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Reviews (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProductId   INT NOT NULL,
    UserId      INT NOT NULL,
    Rating      INT NOT NULL,
    Comment     NVARCHAR(1000) NULL,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users    FOREIGN KEY (UserId)    REFERENCES dbo.Users(Id)    ON DELETE CASCADE,
    CONSTRAINT UQ_Reviews_Product_User UNIQUE (ProductId, UserId)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 18. WISHLIST ITEMS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.WishlistItems (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL,
    ProductId   INT NOT NULL,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_WishlistItems_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT FK_WishlistItems_Users    FOREIGN KEY (UserId)    REFERENCES dbo.Users(Id)    ON DELETE CASCADE,
    CONSTRAINT FK_WishlistItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_WishlistItems_User_Product UNIQUE (UserId, ProductId)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 19. VOUCHERS (mã giảm giá)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.Vouchers (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    Code                NVARCHAR(30) NOT NULL,
    Description         NVARCHAR(200) NULL,
    DiscountType        NVARCHAR(10) NOT NULL DEFAULT ('Percent'),  -- 'Percent' hoặc 'Amount'
    DiscountValue       DECIMAL(18,2) NOT NULL,
    MaxDiscountAmount   DECIMAL(18,2) NULL,
    MinOrderAmount      DECIMAL(18,2) NOT NULL DEFAULT (0),
    StartDate           DATETIME2 NOT NULL DEFAULT (SYSDATETIME()),
    ExpiryDate          DATETIME2 NOT NULL,
    UsageLimit          INT NULL,
    UsedCount           INT NOT NULL DEFAULT (0),
    IsActive            BIT NOT NULL DEFAULT (1),

    CONSTRAINT UQ_Vouchers_Code UNIQUE (Code)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 20. RETURN REQUESTS (trả hàng / hoàn tiền)
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ReturnRequests (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    OrderId           INT NOT NULL,
    UserId            INT NOT NULL,
    RequestType       NVARCHAR(20)   NOT NULL DEFAULT 'Return',
    Reason            NVARCHAR(100)  NOT NULL,
    Description       NVARCHAR(1000) NULL,
    ImageUrls         NVARCHAR(2000) NULL,
    Status            NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    RefundAmount      DECIMAL(18,2)  NULL,
    RefundMethod      NVARCHAR(30)   NULL,
    AdminNote         NVARCHAR(1000) NULL,
    CreatedAt         DATETIME2      NOT NULL DEFAULT GETDATE(),
    ProcessedAt       DATETIME2      NULL,
    ProcessedByUserId INT            NULL,

    CONSTRAINT FK_ReturnRequests_Orders      FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ReturnRequests_Users       FOREIGN KEY (UserId)  REFERENCES dbo.Users(Id),
    CONSTRAINT FK_ReturnRequests_ProcessedBy FOREIGN KEY (ProcessedByUserId) REFERENCES dbo.Users(Id)
);
GO

-- ════════════════════════════════════════════════════════════════
-- 21. RETURN REQUEST ITEMS
-- ════════════════════════════════════════════════════════════════
CREATE TABLE dbo.ReturnRequestItems (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    ReturnRequestId INT NOT NULL,
    OrderDetailId   INT NOT NULL,
    Quantity        INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_ReturnRequestItems_ReturnRequests FOREIGN KEY (ReturnRequestId) REFERENCES dbo.ReturnRequests(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ReturnRequestItems_OrderDetails   FOREIGN KEY (OrderDetailId)   REFERENCES dbo.OrderDetails(Id)
);
GO

PRINT '====================================================';
PRINT 'HOAN THANH: Da tao toan bo 21 bang cho AsusLaptopDB';
PRINT '====================================================';
GO

/* =====================================================================================
   PHẦN SEED DỮ LIỆU MẪU CHO BIẾN THỂ (ProductVariants + SerialNumbers)
   ---------------------------------------------------------------------------------
   LƯU Ý QUAN TRỌNG:
   11 sản phẩm gốc (Id = 1..11) KHÔNG được seed trong SQL — chúng được tự động tạo
   bởi code C# (Data/DbInitializer.cs) khi ứng dụng chạy lần đầu tiên.
   => Phải CHẠY ỨNG DỤNG (F5 / dotnet run) MỘT LẦN để DbInitializer seed 11 sản phẩm
      trước, rồi mới chạy khối lệnh bên dưới để thêm biến thể + serial number mẫu.
   Khối lệnh được bọc trong IF EXISTS để không lỗi nếu Products đang rỗng.
   ===================================================================================== */
IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id BETWEEN 1 AND 11)
BEGIN
    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (1,'16 GB DDR5','Eclipse Gray','#3D3D3D',0,8,1),
    (1,'16 GB DDR5','Volt Green','#7CB518',0,6,0),
    (1,'32 GB DDR5','Eclipse Gray','#3D3D3D',3000000,5,0),
    (1,'32 GB DDR5','Volt Green','#7CB518',3000000,4,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (2,'16 GB LPDDR5X','Eclipse Gray','#3D3D3D',-5000000,4,0),
    (2,'32 GB LPDDR5X','Eclipse Gray','#3D3D3D',0,5,1),
    (2,'32 GB LPDDR5X','Platinum White','#F0F0F0',0,3,0),
    (2,'32 GB LPDDR5X','Nebula Green','#4CAF50',0,3,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (3,'16 GB LPDDR5','Inkwell Black','#1A1A2E',0,6,1),
    (3,'16 GB LPDDR5','Luna White','#F5F5F5',0,5,0),
    (3,'32 GB LPDDR5','Inkwell Black','#1A1A2E',5000000,4,0),
    (3,'32 GB LPDDR5','Luna White','#F5F5F5',5000000,3,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (4,'16 GB DDR4','Mecha Gray','#6B7280',0,15,1),
    (4,'16 GB DDR4','Jaeger Gray','#4B5563',0,12,0),
    (4,'32 GB DDR4','Mecha Gray','#6B7280',2000000,8,0),
    (4,'32 GB DDR4','Jaeger Gray','#4B5563',2000000,5,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (5,'16 GB DDR5','Mecha Gray','#6B7280',0,18,1),
    (5,'16 GB DDR5','Off Black','#2D2D2D',0,15,0),
    (5,'32 GB DDR5','Mecha Gray','#6B7280',2000000,10,0),
    (5,'32 GB DDR5','Off Black','#2D2D2D',2000000,7,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (6,'16 GB LPDDR5X','Ponder Blue','#4A7C9E',-3000000,5,0),
    (6,'32 GB LPDDR5X','Ponder Blue','#4A7C9E',0,7,1),
    (6,'32 GB LPDDR5X','Jasper Slate','#8B7355',0,5,0),
    (6,'32 GB LPDDR5X','Foggy Silver','#C0C0C0',0,4,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (7,'16 GB LPDDR5','Basalt Gray','#5C5C5C',0,8,1),
    (7,'16 GB LPDDR5','Refined White','#EFEFEF',0,7,0),
    (7,'32 GB LPDDR5','Basalt Gray','#5C5C5C',3000000,4,0),
    (7,'32 GB LPDDR5','Refined White','#EFEFEF',3000000,3,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (8,'8 GB DDR4','Midnight Black','#0D0D0D',0,12,1),
    (8,'8 GB DDR4','Quiet Blue','#5C7FA3',0,10,0),
    (8,'16 GB DDR4','Midnight Black','#0D0D0D',2000000,6,0),
    (8,'16 GB DDR4','Quiet Blue','#5C7FA3',2000000,4,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (9,'8 GB DDR4','Indie Black','#1C1C1C',0,15,1),
    (9,'8 GB DDR4','Cool Silver','#A8A8A8',0,12,0),
    (9,'16 GB DDR4','Indie Black','#1C1C1C',1500000,8,0),
    (9,'16 GB DDR4','Cool Silver','#A8A8A8',1500000,6,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (10,'32 GB DDR5','Nano Black','#1A1A1A',-10000000,3,0),
    (10,'64 GB DDR5','Nano Black','#1A1A1A',0,4,1),
    (10,'64 GB DDR5','Star Gray','#707070',0,3,0),
    (10,'96 GB DDR5','Nano Black','#1A1A1A',10000000,2,0);

    INSERT INTO ProductVariants (ProductId,RAM,Color,ColorHex,PriceAdjust,Stock,IsDefault) VALUES
    (11,'16 GB LPDDR5','Star Black','#0A0A0A',-3000000,4,0),
    (11,'32 GB LPDDR5','Star Black','#0A0A0A',0,5,1),
    (11,'32 GB LPDDR5','Pure White','#F8F8F8',0,3,0);

    -- Seed 5 serial number cho mỗi biến thể
    DECLARE @vId INT, @pId INT, @series NVARCHAR(50), @prefix NVARCHAR(3), @i INT, @serial NVARCHAR(30);
    DECLARE cur CURSOR FOR
        SELECT pv.Id, pv.ProductId, p.Series
        FROM ProductVariants pv JOIN Products p ON pv.ProductId = p.Id ORDER BY pv.Id;
    OPEN cur;
    FETCH NEXT FROM cur INTO @vId, @pId, @series;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @prefix = UPPER(LEFT(REPLACE(@series,' ',''), 3));
        SET @i = 1;
        WHILE @i <= 5
        BEGIN
            SET @serial = 'ASU-' + @prefix + '-' + FORMAT(GETDATE(),'yy') + '-' + RIGHT('000000'+CAST(@vId*10+@i AS VARCHAR),6);
            IF NOT EXISTS (SELECT 1 FROM SerialNumbers WHERE SerialNo=@serial)
                INSERT INTO SerialNumbers (SerialNo,ProductId,VariantId,Status)
                VALUES (@serial, @pId, @vId, 'Available');
            SET @i += 1;
        END
        FETCH NEXT FROM cur INTO @vId, @pId, @series;
    END
    CLOSE cur; DEALLOCATE cur;

    PRINT 'Da seed ProductVariants + SerialNumbers cho 11 san pham mau.';
END
ELSE
BEGIN
    PRINT '>>> BO QUA seed bien the: bang Products dang rong.';
    PRINT '>>> Hay chay ung dung (dotnet run) 1 lan de DbInitializer seed 11 san pham,';
    PRINT '>>> sau do chay lai RIENG khoi lenh seed bien the phia tren neu can.';
END
GO

/* =====================================================================================
   THÊM 20 SẢN PHẨM MỚI (nội dung Them_20_SanPham_Moi.sql)
   ---------------------------------------------------------------------------------
   Đây là 20 model laptop hoàn toàn khác với 11 sản phẩm gốc do DbInitializer seed,
   nên KHÔNG ảnh hưởng tới guard "if (Products.Any()) return;" trong DbInitializer.cs
   MIỄN LÀ được chạy SAU KHI app đã seed xong 11 sản phẩm gốc lần đầu (nếu chạy
   script này trước khi app khởi động lần đầu, Products sẽ không còn rỗng nữa và
   DbInitializer sẽ bỏ qua luôn việc seed Users + 11 sản phẩm gốc + tài khoản admin).
   Đã thêm guard IF NOT EXISTS để chạy lại nhiều lần không bị chèn trùng 20 sản phẩm.
   ===================================================================================== */
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = N'ASUS ROG Strix SCAR 18 G834JZ')
BEGIN
    INSERT INTO Products
        (Name, Price, OriginalPrice, ImageUrl, Quantity, Description, Brand, Series,
         ScreenSize, ScreenResolution, CPU, RAM, Storage, GPU, Battery, Weight, OS, CreatedAt, ViewCount)
    VALUES
    ('ASUS ROG Strix SCAR 18 G834JZ', 65990000, 72990000,
     'https://vn.store.asus.com/media/catalog/product/g/8/g834jz-scar18.jpg', 10,
     N'ROG Strix SCAR 18 - flagship gaming màn hình 18 inch Mini LED 240Hz, Intel Core i9-14900HX, RTX 4080 mạnh mẽ. Bàn phím per-key RGB, hệ thống tản nhiệt Conductonaut Extreme, dành cho game thủ chuyên nghiệp và streamer.',
     'ASUS', 'ROG Strix', '18 inch', 'QHD+ Mini LED 240Hz', 'Intel Core i9-14900HX', '32 GB DDR5', '1 TB NVMe SSD',
     'NVIDIA GeForce RTX 4080 12GB', '90 WHr', '3.1 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ROG Strix G17 G713PV', 38990000, 44990000,
     'https://vn.store.asus.com/media/catalog/product/g/7/g713pv-strix17.jpg', 22,
     N'ROG Strix G17 sở hữu màn hình 17.3 inch 165Hz rộng rãi, AMD Ryzen 9 7940HX cùng RTX 4060, thích hợp cho game thủ ưa màn hình lớn. Tản nhiệt kép Tri-Fan, bàn phím RGB, khung máy chắc chắn.',
     'ASUS', 'ROG Strix', '17.3 inch', 'FHD 165Hz', 'AMD Ryzen 9 7940HX', '16 GB DDR5', '1 TB NVMe SSD',
     'NVIDIA GeForce RTX 4060 8GB', '90 WHr', '2.8 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ROG Zephyrus M16 GU604VI', 48990000, 55000000,
     'https://vn.store.asus.com/media/catalog/product/g/u/gu604vi-zephyrusm16.jpg', 14,
     N'ROG Zephyrus M16 kết hợp thiết kế sang trọng với hiệu năng mạnh mẽ, màn hình 16 inch QHD+ 240Hz, Intel Core i9-13900H và RTX 4070. Loa 6 củ Dolby Atmos, bàn phím có đèn nền RGB per-key.',
     'ASUS', 'ROG Zephyrus', '16 inch', 'QHD+ 240Hz', 'Intel Core i9-13900H', '32 GB DDR5', '1 TB NVMe SSD',
     'NVIDIA GeForce RTX 4070 8GB', '90 WHr', '1.95 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ROG Zephyrus Duo 16 GX650PZ', 89990000, 99990000,
     'https://vn.store.asus.com/media/catalog/product/g/x/gx650pz-duo16.jpg', 6,
     N'ROG Zephyrus Duo 16 với màn hình phụ ROG ScreenPad Plus độc quyền, AMD Ryzen 9 7945HX3D cùng RTX 4090 đỉnh cao. Thiết kế hai màn hình mở ra không gian làm việc đa nhiệm chưa từng có cho game thủ và creator.',
     'ASUS', 'ROG Zephyrus', '16 inch', 'QHD+ Mini LED 240Hz', 'AMD Ryzen 9 7945HX3D', '64 GB DDR5', '2 TB NVMe SSD',
     'NVIDIA GeForce RTX 4090 16GB', '90 WHr', '2.5 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ROG Flow Z13 GZ302EA', 46990000, 52990000,
     'https://vn.store.asus.com/media/catalog/product/g/z/gz302ea-flowz13.jpg', 12,
     N'ROG Flow Z13 - máy tính bảng gaming 2-in-1 mỏng nhẹ với AMD Ryzen AI 9, đồ họa Radeon tích hợp mạnh mẽ, màn hình cảm ứng 165Hz. Có thể kết hợp ROG XG Mobile để nâng cấp hiệu năng đồ họa rời.',
     'ASUS', 'ROG Flow', '13.4 inch', 'WUXGA 165Hz Touch', 'AMD Ryzen AI 9 HX370', '32 GB LPDDR5X', '1 TB NVMe SSD',
     'AMD Radeon 890M', '56 WHr', '1.18 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS TUF Gaming F17 FX707VU', 26990000, 30990000,
     'https://vn.store.asus.com/media/catalog/product/f/x/fx707vu-tuf17.jpg', 28,
     N'TUF Gaming F17 màn hình lớn 17.3 inch 144Hz, Intel Core i7-13620H kết hợp RTX 4060, thiết kế bền bỉ chuẩn quân đội MIL-STD-810H. Lựa chọn lý tưởng cho game thủ cần không gian màn hình rộng với mức giá hợp lý.',
     'ASUS', 'TUF Gaming', '17.3 inch', 'FHD 144Hz', 'Intel Core i7-13620H', '16 GB DDR5', '512 GB NVMe SSD',
     'NVIDIA GeForce RTX 4060 8GB', '90 WHr', '2.6 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS TUF Gaming A17 FA707NU', 21990000, 25990000,
     'https://vn.store.asus.com/media/catalog/product/f/a/fa707nu-tufa17.jpg', 35,
     N'TUF Gaming A17 trang bị AMD Ryzen 7 7735HS cùng RTX 4050, màn hình 144Hz mượt mà. Vỏ máy nhôm-nhựa bền bỉ, bàn phím chống tràn nước, phù hợp game thủ vừa học vừa chơi.',
     'ASUS', 'TUF Gaming', '17.3 inch', 'FHD 144Hz', 'AMD Ryzen 7 7735HS', '16 GB DDR5', '512 GB NVMe SSD',
     'NVIDIA GeForce RTX 4050 6GB', '90 WHr', '2.6 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS TUF Dash F15 FX517ZR', 29990000, 34990000,
     'https://vn.store.asus.com/media/catalog/product/f/x/fx517zr-dashf15.jpg', 20,
     N'TUF Dash F15 - phiên bản mỏng nhẹ trong dòng TUF Gaming, chỉ 2.0 kg, Intel Core i7-12650H và RTX 3070 Ti mạnh mẽ. Màn hình 144Hz, thân máy nhôm sang trọng nhưng vẫn đạt chuẩn bền MIL-STD.',
     'ASUS', 'TUF Gaming', '15.6 inch', 'FHD 144Hz', 'Intel Core i7-12650H', '16 GB DDR5', '1 TB NVMe SSD',
     'NVIDIA GeForce RTX 3070 Ti 8GB', '76 WHr', '2.0 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ZenBook Pro 16X OLED UX7602ZM', 58990000, 65000000,
     'https://cdn.ankhang.vn/media/product/ux7602zm-zenbookpro16x.jpg', 9,
     N'ZenBook Pro 16X OLED dành cho nhà sáng tạo nội dung chuyên nghiệp. Màn hình 4K OLED cảm ứng, bàn phím ẩn tự nâng AAS Ultra, Intel Core i9-12900H cùng RTX 3060 xử lý mượt mọi tác vụ dựng phim, đồ họa.',
     'ASUS', 'ZenBook', '16 inch', '4K OLED 120Hz Touch', 'Intel Core i9-12900H', '32 GB DDR5', '1 TB NVMe SSD',
     'NVIDIA GeForce RTX 3060 6GB', '96 WHr', '2.4 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS ZenBook Flip S 13 OLED UX371EA', 27990000, 32990000,
     'https://cdn.ankhang.vn/media/product/ux371ea-zenbookflips13.jpg', 25,
     N'ZenBook Flip S 13 OLED - laptop 2-in-1 xoay gập 360°, màn OLED 4K cảm ứng sắc nét, thân máy hợp kim mạ kim cương sang trọng chỉ 1.2 kg. Đi kèm bút cảm ứng ASUS Pen tiện lợi cho ghi chú, vẽ phác thảo.',
     'ASUS', 'ZenBook', '13.3 inch', '4K OLED 60Hz Touch', 'Intel Core i7-1165G7', '16 GB LPDDR4X', '512 GB NVMe SSD',
     'Intel Iris Xe Graphics', '67 WHr', '1.2 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ZenBook Duo 14 UX8406MA', 44990000, 50990000,
     'https://cdn.ankhang.vn/media/product/ux8406ma-zenbookduo14.jpg', 13,
     N'ZenBook Duo 14 sở hữu hai màn hình OLED 2.8K độc đáo, tăng gấp đôi không gian làm việc. Intel Core Ultra 9 tích hợp NPU AI mạnh mẽ, bàn phím rời kèm theo tiện lợi mang đi làm việc mọi nơi.',
     'ASUS', 'ZenBook', '14 inch', '2.8K OLED 120Hz Touch', 'Intel Core Ultra 9 185H', '32 GB LPDDR5X', '1 TB NVMe SSD',
     'Intel Arc Graphics', '75 WHr', '1.65 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS VivoBook Pro 15 OLED K6502VU', 24990000, 28990000,
     'https://phucanhcdn.com/media/product/k6502vu-vivobookpro15.jpg', 32,
     N'VivoBook Pro 15 OLED dành cho người dùng sáng tạo phổ thông. Màn OLED 2.8K rực rỡ, Intel Core i5-13500H kết hợp RTX 4050, thiết kế nắp máy nhôm CNC tinh tế, phù hợp thiết kế đồ họa nhẹ và giải trí.',
     'ASUS', 'VivoBook', '15.6 inch', '2.8K OLED 60Hz', 'Intel Core i5-13500H', '16 GB DDR4', '512 GB NVMe SSD',
     'NVIDIA GeForce RTX 4050 6GB', '70 WHr', '1.7 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS VivoBook S15 OLED K3502ZA', 18990000, 21990000,
     'https://phucanhcdn.com/media/product/k3502za-vivobooks15.jpg', 45,
     N'VivoBook S15 OLED trang bị màn OLED FHD sắc nét, Intel Core i5-1240P xử lý mượt mà mọi tác vụ văn phòng, học tập. Thiết kế mỏng 1.6 kg, nhiều màu sắc trẻ trung, thời lượng pin cả ngày dài.',
     'ASUS', 'VivoBook', '15.6 inch', 'FHD OLED 60Hz', 'Intel Core i5-1240P', '8 GB DDR4', '512 GB NVMe SSD',
     'Intel Iris Xe Graphics', '50 WHr', '1.6 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS VivoBook Go 15 E1504FA', 10990000, 12990000,
     'https://phucanhcdn.com/media/product/e1504fa-vivobookgo15.jpg', 55,
     N'VivoBook Go 15 - laptop giá rẻ dành cho học sinh sinh viên. AMD Ryzen 5 7430U tiết kiệm điện, thiết kế nhẹ nhàng dễ mang theo, đáp ứng tốt nhu cầu học online, soạn thảo văn bản, lướt web hàng ngày.',
     'ASUS', 'VivoBook', '15.6 inch', 'FHD 60Hz', 'AMD Ryzen 5 7430U', '8 GB LPDDR5', '512 GB NVMe SSD',
     'AMD Radeon Graphics', '42 WHr', '1.63 kg', 'Windows 11 Home', GETDATE(), 0),

    ('ASUS ProArt StudioBook Pro 17 W7604H', 92990000, 105000000,
     'https://vn.store.asus.com/media/catalog/product/w/7/w7604h-proart17.jpg', 5,
     N'ProArt StudioBook Pro 17 - trạm làm việc di động mạnh mẽ nhất dành cho kỹ sư, nhà làm phim chuyên nghiệp. Màn hình 4K OLED chuẩn màu Pantone, Intel Core i9-13980HX cùng RTX 4090 xử lý render, mô phỏng 3D siêu nhanh.',
     'ASUS', 'ProArt', '17.3 inch', '4K OLED 120Hz', 'Intel Core i9-13980HX', '64 GB DDR5', '2 TB NVMe SSD',
     'NVIDIA GeForce RTX 4090 16GB', '90 WHr', '3.0 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS ProArt PX13 HN7306W', 39990000, 45990000,
     'https://vn.store.asus.com/media/catalog/product/h/n/hn7306w-proartpx13.jpg', 16,
     N'ProArt PX13 - laptop sáng tạo mỏng nhẹ dành cho dân thiết kế di động. AMD Ryzen AI 9 tích hợp NPU mạnh, màn hình OLED cảm ứng chuẩn màu, đi kèm bảng màu ASUS Dial vật lý hỗ trợ chỉnh sửa chính xác.',
     'ASUS', 'ProArt', '13.3 inch', '2.8K OLED 120Hz Touch', 'AMD Ryzen AI 9 HX370', '32 GB LPDDR5X', '1 TB NVMe SSD',
     'AMD Radeon 890M', '68 WHr', '1.3 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS ExpertBook B5 Flip B5302FBA', 32990000, 37990000,
     'https://cdn.ankhang.vn/media/product/b5302fba-expertbookb5flip.jpg', 19,
     N'ExpertBook B5 Flip - laptop doanh nhân 2-in-1 xoay gập linh hoạt, bảo mật vân tay và IR Camera, khung máy hợp kim ma giê siêu bền đạt 12 chứng nhận MIL-STD-810H. Đi kèm bút cảm ứng tiện lợi cho công việc.',
     'ASUS', 'ExpertBook', '13.3 inch', 'FHD Touch', 'Intel Core i7-1355U', '16 GB LPDDR5', '512 GB NVMe SSD',
     'Intel Iris Xe Graphics', '63 WHr', '1.3 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS ExpertBook P5 P5405CSA', 19990000, 22990000,
     'https://cdn.ankhang.vn/media/product/p5405csa-expertbookp5.jpg', 27,
     N'ExpertBook P5 - laptop văn phòng bền bỉ giá tốt, Intel Core i5 thế hệ 13, khung máy đạt chuẩn quân đội, bảo mật TPM 2.0 và vân tay. Phù hợp doanh nghiệp vừa và nhỏ cần trang bị số lượng lớn.',
     'ASUS', 'ExpertBook', '14 inch', 'FHD 60Hz', 'Intel Core i5-1335U', '8 GB DDR4', '512 GB NVMe SSD',
     'Intel UHD Graphics', '50 WHr', '1.4 kg', 'Windows 11 Pro', GETDATE(), 0),

    ('ASUS Chromebook Plus CX34 Flip CX3402', 13990000, 15990000,
     'https://cdn.ankhang.vn/media/product/cx3402-chromebookplus.jpg', 38,
     N'Chromebook Plus CX34 Flip chạy ChromeOS mượt mà, tích hợp AI Google Gemini hỗ trợ học tập, làm việc. Thiết kế xoay gập 360°, màn hình cảm ứng, pin bền tới 12 tiếng, khởi động chỉ trong vài giây.',
     'ASUS', 'Chromebook', '14 inch', 'FHD 60Hz Touch', 'Intel Core i3-1215U', '8 GB LPDDR5', '128 GB eMMC',
     'Intel UHD Graphics', '50 WHr', '1.5 kg', 'ChromeOS', GETDATE(), 0),

    ('ASUS Vivobook 14 X1404ZA', 12990000, 14990000,
     'https://phucanhcdn.com/media/product/x1404za-vivobook14.jpg', 48,
     N'Vivobook 14 X1404ZA nhỏ gọn, nhẹ nhàng chỉ 1.4 kg, Intel Core i3 thế hệ 12 xử lý tốt các tác vụ cơ bản, học tập, văn phòng. Thiết kế trẻ trung nhiều màu sắc, mức giá phù hợp học sinh, sinh viên.',
     'ASUS', 'VivoBook', '14 inch', 'FHD 60Hz', 'Intel Core i3-1215U', '8 GB DDR4', '256 GB NVMe SSD',
     'Intel UHD Graphics', '42 WHr', '1.4 kg', 'Windows 11 Home', GETDATE(), 0);

    PRINT 'Da them 20 san pham moi vao Products.';
END
ELSE
BEGIN
    PRINT '>>> BO QUA: 20 san pham moi da ton tai san, khong chen trung.';
END
GO

/* =====================================================================================
   ĐỒNG BỘ LẠI Product.Quantity = tổng Stock của các biến thể (nếu sản phẩm có biến thể)
   An toàn khi chạy nhiều lần / chạy khi chưa có biến thể (không có gì để cập nhật).
   ===================================================================================== */
UPDATE p
SET p.Quantity = v.TotalStock
FROM Products p
INNER JOIN (
    SELECT ProductId, SUM(Stock) AS TotalStock
    FROM ProductVariants
    GROUP BY ProductId
) v ON v.ProductId = p.Id;
GO

-- ════════════════════════════════════════════════════════════════
-- KIỂM TRA KẾT QUẢ
-- ════════════════════════════════════════════════════════════════
SELECT
    t.name      AS [Ten bang],
    SUM(p.rows) AS [So dong]
FROM sys.tables t
JOIN sys.indexes i        ON t.object_id = i.object_id
JOIN sys.partitions p     ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name
ORDER BY t.name;
GO

PRINT 'Database AsusLaptopDB created successfully! (script gop tu 7 file goc)';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Email, FullName, Phone, Role, CreatedAt)
    VALUES ('admin', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=',
            'admin@asuslaptop.vn', N'Administrator', '0987654321', 'Admin', DATEADD(DAY,-30,GETDATE()));
    PRINT 'Da tao tai khoan admin (admin / admin123)';
END
ELSE
BEGIN
    -- Nếu username admin đã tồn tại nhưng sai hash/pass, reset lại về admin123
    UPDATE Users SET PasswordHash = 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', Role = 'Admin'
    WHERE Username = 'admin';
    PRINT 'Tai khoan admin da ton tai -> da reset mat khau ve admin123';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'customer')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Email, FullName, Phone, Role, CreatedAt)
    VALUES ('customer', 'sEHArrNbsPpKpmjKWpILWQGW/a+aAOuFLJt/TRI8xtY=',
            'customer@gmail.com', N'Nguyễn Văn A', '0912345678', 'Customer', DATEADD(DAY,-15,GETDATE()));
    PRINT 'Da tao tai khoan customer (customer / customer123)';
END
GO

SELECT Id, Username, Email, Role FROM Users;



CREATE TABLE [ProductRegistrations] (
    [Id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId]        INT NULL,
    [SerialNo]      NVARCHAR(30)  NOT NULL,
    [ProductId]     INT NOT NULL,
    [FullName]      NVARCHAR(100) NOT NULL,
    [Phone]         NVARCHAR(20)  NOT NULL,
    [Email]         NVARCHAR(100) NULL,
    [PurchaseDate]  DATETIME2     NOT NULL,
    [PurchasePlace] NVARCHAR(200) NULL,
    [RegisteredAt]  DATETIME2     NOT NULL DEFAULT (GETDATE()),
    [Status]        NVARCHAR(20)  NOT NULL DEFAULT ('Approved'),
    [Note]          NVARCHAR(MAX) NULL,

    CONSTRAINT [FK_ProductRegistrations_Users]
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_ProductRegistrations_Products]
        FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id])
);

CREATE INDEX [IX_ProductRegistrations_SerialNo] ON [ProductRegistrations]([SerialNo]);
CREATE INDEX [IX_ProductRegistrations_UserId] ON [ProductRegistrations]([UserId]);
