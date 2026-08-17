-- ============================================================
-- CHỨC NĂNG "MUA TRƯỚC - TRẢ SAU" TÍCH HỢP VÍ TRẢ SAU MOMO
-- Thêm cột cho bảng Orders trên database AsusLaptopDB
-- ============================================================

use AsusLaptopDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'IsPayLater')
BEGIN
    ALTER TABLE dbo.Orders ADD IsPayLater BIT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm cột IsPayLater vào bảng Orders';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'MoMoOrderId')
BEGIN
    ALTER TABLE dbo.Orders ADD MoMoOrderId NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột MoMoOrderId vào bảng Orders';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'MoMoTransId')
BEGIN
    ALTER TABLE dbo.Orders ADD MoMoTransId NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột MoMoTransId vào bảng Orders';
END
GO

-- Kiểm tra lại
SELECT TOP 20 Id, PaymentMethod, PaymentStatus, IsPayLater, MoMoOrderId, MoMoTransId FROM Orders ORDER BY Id DESC;
