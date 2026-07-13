CREATE DATABASE GiaHoaPhatEBusiness;
GO
USE GiaHoaPhatEBusiness;
GO

CREATE TABLE Role (
    RoleId TINYINT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId TINYINT NOT NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    Phone VARCHAR(20),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (RoleId) REFERENCES Role(RoleId)
);

CREATE TABLE UserAddress (
    AddressId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ReceiverName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    StreetAddress NVARCHAR(255) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    District NVARCHAR(100) NOT NULL,
    Ward NVARCHAR(100) NOT NULL,
    IsDefault BIT DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES [User](UserId)
);

CREATE TABLE Category (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    ParentCategoryId INT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (ParentCategoryId) REFERENCES Category(CategoryId)
);

CREATE TABLE Product (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    ProductName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    UnitOfMeasure NVARCHAR(20) NOT NULL,
    Weight DECIMAL(10,2) NOT NULL CHECK (Weight >= 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    TotalStock INT NOT NULL DEFAULT 0 CHECK (TotalStock >= 0),
    ReservedQuantity INT NOT NULL DEFAULT 0 CHECK (ReservedQuantity >= 0),
    IsDeleted BIT DEFAULT 0,
    RowVersion ROWVERSION,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId)
);

CREATE TABLE ProductImage (
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl VARCHAR(255) NOT NULL,
    IsPrimary BIT DEFAULT 0,
    SortOrder TINYINT DEFAULT 0,
    FOREIGN KEY (ProductId) REFERENCES Product(ProductId) ON DELETE CASCADE
);

CREATE TABLE InventoryBatch (
    BatchId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    BatchCode VARCHAR(50) NOT NULL,
    ManufactureDate DATE NOT NULL,
    ExpiryDate DATE NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity >= 0),
    CostPrice DECIMAL(18,2) NOT NULL CHECK (CostPrice >= 0),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (ProductId) REFERENCES Product(ProductId)
);

CREATE TABLE Voucher (
    VoucherId INT IDENTITY(1,1) PRIMARY KEY,
    VoucherCode VARCHAR(50) NOT NULL UNIQUE,
    DiscountType TINYINT NOT NULL,
    DiscountValue DECIMAL(18,2) NOT NULL CHECK (DiscountValue > 0),
    MinOrderValue DECIMAL(18,2) DEFAULT 0,
    MaxDiscountAmount DECIMAL(18,2),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    UsageLimit INT NOT NULL CHECK (UsageLimit >= 0),
    UsedCount INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Cart (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT UNIQUE,
    SessionId VARCHAR(100) UNIQUE,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    CONSTRAINT CHK_Cart_Identity CHECK (UserId IS NOT NULL OR SessionId IS NOT NULL)
);

CREATE TABLE CartItem (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    FOREIGN KEY (CartId) REFERENCES Cart(CartId),
    FOREIGN KEY (ProductId) REFERENCES Product(ProductId),
    CONSTRAINT UQ_Cart_Product UNIQUE (CartId, ProductId)
);

CREATE TABLE [Order] (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    VoucherId INT NULL,
    OrderDate DATETIME2 DEFAULT GETUTCDATE(),
    SubTotal DECIMAL(18,2) NOT NULL CHECK (SubTotal >= 0),
    ShippingFee DECIMAL(18,2) NOT NULL CHECK (ShippingFee >= 0),
    DiscountAmount DECIMAL(18,2) DEFAULT 0 CHECK (DiscountAmount >= 0),
    TotalAmount DECIMAL(18,2) NOT NULL CHECK (TotalAmount >= 0),
    ShippingAddress NVARCHAR(500) NOT NULL,
    OrderStatus TINYINT NOT NULL,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    FOREIGN KEY (VoucherId) REFERENCES Voucher(VoucherId)
);

CREATE TABLE OrderDetail (
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductNameSnapshot NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    LineTotal AS (Quantity * UnitPrice),
    PRIMARY KEY (OrderId, ProductId),
    FOREIGN KEY (OrderId) REFERENCES [Order](OrderId),
    FOREIGN KEY (ProductId) REFERENCES Product(ProductId)
);

CREATE TABLE Payment (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL UNIQUE,
    ProviderTransactionId VARCHAR(100),
    PaymentMethod TINYINT NOT NULL,
    PaymentDate DATETIME2 DEFAULT GETUTCDATE(),
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount >= 0),
    TransactionStatus TINYINT NOT NULL,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (OrderId) REFERENCES [Order](OrderId)
);

CREATE TABLE Shipment (
    ShipmentId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL UNIQUE,
    LogisticsProvider VARCHAR(100) NOT NULL,
    TrackingNumber VARCHAR(100),
    TotalWeight DECIMAL(10,2) NOT NULL CHECK (TotalWeight >= 0),
    EstimatedDeliveryDate DATETIME2,
    ShipmentStatus TINYINT NOT NULL,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (OrderId) REFERENCES [Order](OrderId)
);

CREATE TABLE Review (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    OrderId INT NOT NULL,
    Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX),
    ReviewDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    FOREIGN KEY (ProductId) REFERENCES Product(ProductId),
    FOREIGN KEY (OrderId) REFERENCES [Order](OrderId),
    CONSTRAINT UQ_Review_Order_Product UNIQUE (OrderId, ProductId)
);

CREATE NONCLUSTERED INDEX IX_User_Email ON [User](Email);
CREATE NONCLUSTERED INDEX IX_Product_CategoryId ON Product(CategoryId);
CREATE NONCLUSTERED INDEX IX_Product_SKU ON Product(SKU);
CREATE NONCLUSTERED INDEX IX_Order_UserId ON [Order](UserId);
CREATE NONCLUSTERED INDEX IX_Cart_SessionId ON Cart(SessionId);
CREATE NONCLUSTERED INDEX IX_CartItem_CartId ON CartItem(CartId);
CREATE NONCLUSTERED INDEX IX_Review_ProductId ON Review(ProductId);
CREATE NONCLUSTERED INDEX IX_InventoryBatch_ExpiryDate ON InventoryBatch(ExpiryDate);

USE GiaHoaPhatEBusiness;
GO

-- 1. THÊM DỮ LIỆU ROLE
INSERT INTO Role (RoleName) 
VALUES ('Admin'), ('Customer');
GO

-- 2. THÊM DỮ LIỆU USER
INSERT INTO [User] (RoleId, Username, PasswordHash, FullName, Email, Phone) 
VALUES 
(1, 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', N'Quản Trị Viên', 'admin@giahoaphat.com', '0988888888'),
(2, 'nguyenvana', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', N'Thợ Bánh Mì', 'thobanh@gmail.com', '0909090909');
GO

-- 3. THÊM DỮ LIỆU CATEGORY (5 Danh mục nguyên liệu/dụng cụ)
SET IDENTITY_INSERT Category ON;
INSERT INTO Category (CategoryId, ParentCategoryId, CategoryName, Description) VALUES
(1, NULL, N'Bột các loại', N'Bột mì đa dụng, bột làm bánh mì, bột làm bánh ngọt, bột hạnh nhân...'),
(2, NULL, N'Bơ, Sữa & Phô mai', N'Các loại bơ lạt, whipping cream, topping cream, cream cheese...'),
(3, NULL, N'Đường & Hương liệu', N'Đường bột, đường đen, tinh chất vani, bột nở, muối nở...'),
(4, NULL, N'Sô cô la & Hạt, Trái cây', N'Sô cô la chip, bột cacao, hạnh nhân lát, nho khô...'),
(5, NULL, N'Dụng cụ làm bánh', N'Khuôn khay, phới lồng, máy đánh trứng, tấm nhào bột...');
SET IDENTITY_INSERT Category OFF;
GO

-- 4. THÊM DỮ LIỆU PRODUCT (20 Sản phẩm)
SET IDENTITY_INSERT Product ON;
INSERT INTO Product (ProductId, CategoryId, SKU, ProductName, Description, UnitOfMeasure, Weight, UnitPrice, TotalStock, ReservedQuantity) VALUES
-- Danh mục 1: Bột các loại (4 SP)
(1, 1, 'BOT-MDD-01', N'Bột Mì Đa Dụng Hoa Hồng (1kg)', N'Phù hợp làm nhiều loại bánh quy, bánh bao, bánh ngọt cơ bản.', N'Gói', 1.00, 22000, 150, 0),
(2, 1, 'BOT-S11-01', N'Bột Số 11 (Bột Làm Bánh Mì) (1kg)', N'Hàm lượng protein cao, giúp bánh mì dai ngon, thớ bánh chuẩn.', N'Gói', 1.00, 25000, 120, 0),
(3, 1, 'BOT-S8-01', N'Bột Số 8 (Bột Bánh Bông Lan) (1kg)', N'Siêu mịn, protein thấp giúp bánh bông lan xốp, mềm mại.', N'Gói', 1.00, 26000, 100, 0),
(4, 1, 'BOT-HAN-01', N'Bột Hạnh Nhân Cao Cấp (500g)', N'Nguyên liệu không thể thiếu để làm bánh Macaron chuẩn Pháp.', N'Gói', 0.50, 185000, 50, 0),

-- Danh mục 2: Bơ, Sữa & Phô mai (4 SP)
(5, 2, 'BO-ANC-250', N'Bơ Lạt Anchor (250g)', N'Bơ động vật nguyên chất từ New Zealand, thơm béo tự nhiên.', N'Khối', 0.25, 65000, 80, 0),
(6, 2, 'WHIP-ANC-1L', N'Kem Tươi Whipping Cream Anchor (1 Lít)', N'Dùng để chà láng bánh kem, làm mousse, pha chế đồ uống.', N'Hộp', 1.00, 145000, 60, 0),
(7, 2, 'CREAM-PHI-250', N'Cream Cheese Philadelphia (250g)', N'Phô mai kem cao cấp, chuyên dùng làm Cheesecake, Tiramisu.', N'Hộp', 0.25, 95000, 40, 0),
(8, 2, 'SUA-TH-1L', N'Sữa Tươi Không Đường TH True Milk (1 Lít)', N'Thành phần cơ bản cho các công thức bánh, Flan, Pudding.', N'Hộp', 1.00, 36000, 200, 0),

-- Danh mục 3: Đường & Hương liệu (4 SP)
(9, 3, 'DUONG-ICING', N'Đường Bột Icing Sugar (500g)', N'Đường xay siêu mịn, dùng làm kem bơ hoặc rắc trang trí.', N'Gói', 0.50, 25000, 100, 0),
(10, 3, 'VANI-RAY-28', N'Tinh Chất Vani Rayner''s (28ml)', N'Khử mùi tanh của trứng, tạo hương thơm đặc trưng cho bánh.', N'Chai', 0.05, 35000, 150, 0),
(11, 3, 'DUONG-DEN-1', N'Đường Đen Hàn Quốc (1kg)', N'Dùng làm trân châu đường đen, bánh trung thu, tạo màu tự nhiên.', N'Gói', 1.00, 65000, 80, 0),
(12, 3, 'BAKING-POW', N'Bột Nở Baking Powder Rumford (114g)', N'Không chứa nhôm (Aluminum free), giúp bánh nở đều, xốp nhẹ.', N'Hộp', 0.11, 55000, 90, 0),

-- Danh mục 4: Sô cô la & Trái cây (4 SP)
(13, 4, 'CHOCO-65-1K', N'Sô Cô La Đen Nguyên Chất 65% Puratos (1kg)', N'Dạng nút tròn dễ tan chảy, dùng làm Nama chocolate, phủ bánh.', N'Gói', 1.00, 195000, 45, 0),
(14, 4, 'CHOCO-CHIP', N'Sô Cô La Chip Đen Nướng Không Chảy (500g)', N'Trộn vào cốt bánh quy, muffin, giữ nguyên hạt khi nướng.', N'Gói', 0.50, 75000, 80, 0),
(15, 4, 'NHO-KHO-500', N'Nho Khô Vàng Mỹ Hữu Cơ (500g)', N'Quả to, không hạt, ngọt thanh, dùng làm bánh mì hoa cúc.', N'Gói', 0.50, 95000, 70, 0),
(16, 4, 'HANH-NHAN-L', N'Hạnh Nhân Lát Nướng Sẵn (250g)', N'Trang trí mặt bánh, làm kẹo Nougat, bánh ngói hạnh nhân.', N'Gói', 0.25, 85000, 65, 0),

-- Danh mục 5: Dụng cụ làm bánh (4 SP)
(17, 5, 'DC-PHOI-25', N'Phới Lồng Đánh Trứng Inox (25cm)', N'Thiết kế tay cầm chắc chắn, sợi inox dày dặn, không gỉ.', N'Cái', 0.15, 45000, 120, 0),
(18, 5, 'DC-KHUON-20', N'Khuôn Bánh Tròn Đế Rời Chống Dính (20cm)', N'Chuyên làm bánh bông lan, cheesecake, dễ dàng tháo bánh.', N'Cái', 0.30, 85000, 60, 0),
(19, 5, 'DC-TAM-NH', N'Tấm Nhào Bột Silicon Có Vạch Chia (60x40cm)', N'Chống dính tuyệt đối, chịu nhiệt tốt, cuộn gọn dễ dàng.', N'Cái', 0.20, 95000, 80, 0),
(20, 5, 'DC-CAN-5KG', N'Cân Điện Tử Tiểu Ly Mini (1g - 5kg)', N'Độ chính xác cao, có chức năng trừ bì (Tare) rất tiện dụng.', N'Cái', 0.40, 110000, 50, 0);
SET IDENTITY_INSERT Product OFF;
GO

-- 5. THÊM DỮ LIỆU PRODUCT IMAGES (Từ 2-3 ảnh cho mỗi sản phẩm)
-- Đổi seed thành "baking" để url sinh ra hình ảnh có chủ đề ngẫu nhiên nhưng URL nhìn chuẩn
INSERT INTO ProductImage (ProductId, ImageUrl, IsPrimary, SortOrder) VALUES
-- SP 1: Bột mì đa dụng
(1, 'https://picsum.photos/seed/baking_flour1_1/800/800', 1, 1),
(1, 'https://picsum.photos/seed/baking_flour1_2/800/800', 0, 2),
(1, 'https://picsum.photos/seed/baking_flour1_3/800/800', 0, 3),

-- SP 2: Bột số 11
(2, 'https://picsum.photos/seed/baking_breadflour1_1/800/800', 1, 1),
(2, 'https://picsum.photos/seed/baking_breadflour1_2/800/800', 0, 2),

-- SP 3: Bột số 8
(3, 'https://picsum.photos/seed/baking_cakeflour1_1/800/800', 1, 1),
(3, 'https://picsum.photos/seed/baking_cakeflour1_2/800/800', 0, 2),
(3, 'https://picsum.photos/seed/baking_cakeflour1_3/800/800', 0, 3),

-- SP 4: Bột hạnh nhân
(4, 'https://picsum.photos/seed/baking_almondflour_1/800/800', 1, 1),
(4, 'https://picsum.photos/seed/baking_almondflour_2/800/800', 0, 2),

-- SP 5: Bơ lạt
(5, 'https://picsum.photos/seed/baking_butter1_1/800/800', 1, 1),
(5, 'https://picsum.photos/seed/baking_butter1_2/800/800', 0, 2),
(5, 'https://picsum.photos/seed/baking_butter1_3/800/800', 0, 3),

-- SP 6: Whipping cream
(6, 'https://picsum.photos/seed/baking_whip1_1/800/800', 1, 1),
(6, 'https://picsum.photos/seed/baking_whip1_2/800/800', 0, 2),

-- SP 7: Cream Cheese
(7, 'https://picsum.photos/seed/baking_creamcheese1_1/800/800', 1, 1),
(7, 'https://picsum.photos/seed/baking_creamcheese1_2/800/800', 0, 2),
(7, 'https://picsum.photos/seed/baking_creamcheese1_3/800/800', 0, 3),

-- SP 8: Sữa tươi
(8, 'https://picsum.photos/seed/baking_milk1_1/800/800', 1, 1),
(8, 'https://picsum.photos/seed/baking_milk1_2/800/800', 0, 2),

-- SP 9: Đường Icing
(9, 'https://picsum.photos/seed/baking_icing1_1/800/800', 1, 1),
(9, 'https://picsum.photos/seed/baking_icing1_2/800/800', 0, 2),
(9, 'https://picsum.photos/seed/baking_icing1_3/800/800', 0, 3),

-- SP 10: Vani
(10, 'https://picsum.photos/seed/baking_vani1_1/800/800', 1, 1),
(10, 'https://picsum.photos/seed/baking_vani1_2/800/800', 0, 2),

-- SP 11: Đường đen
(11, 'https://picsum.photos/seed/baking_bsugar1_1/800/800', 1, 1),
(11, 'https://picsum.photos/seed/baking_bsugar1_2/800/800', 0, 2),

-- SP 12: Bột nở
(12, 'https://picsum.photos/seed/baking_bpow1_1/800/800', 1, 1),
(12, 'https://picsum.photos/seed/baking_bpow1_2/800/800', 0, 2),
(12, 'https://picsum.photos/seed/baking_bpow1_3/800/800', 0, 3),

-- SP 13: Socola đen
(13, 'https://picsum.photos/seed/baking_dchoco1_1/800/800', 1, 1),
(13, 'https://picsum.photos/seed/baking_dchoco1_2/800/800', 0, 2),

-- SP 14: Socola chip
(14, 'https://picsum.photos/seed/baking_cchip1_1/800/800', 1, 1),
(14, 'https://picsum.photos/seed/baking_cchip1_2/800/800', 0, 2),
(14, 'https://picsum.photos/seed/baking_cchip1_3/800/800', 0, 3),

-- SP 15: Nho khô
(15, 'https://picsum.photos/seed/baking_raisin1_1/800/800', 1, 1),
(15, 'https://picsum.photos/seed/baking_raisin1_2/800/800', 0, 2),

-- SP 16: Hạnh nhân lát
(16, 'https://picsum.photos/seed/baking_almonds1_1/800/800', 1, 1),
(16, 'https://picsum.photos/seed/baking_almonds1_2/800/800', 0, 2),
(16, 'https://picsum.photos/seed/baking_almonds1_3/800/800', 0, 3),

-- SP 17: Phới lồng
(17, 'https://picsum.photos/seed/baking_whisk1_1/800/800', 1, 1),
(17, 'https://picsum.photos/seed/baking_whisk1_2/800/800', 0, 2),

-- SP 18: Khuôn bánh
(18, 'https://picsum.photos/seed/baking_mold1_1/800/800', 1, 1),
(18, 'https://picsum.photos/seed/baking_mold1_2/800/800', 0, 2),

-- SP 19: Tấm nhào bột
(19, 'https://picsum.photos/seed/baking_mat1_1/800/800', 1, 1),
(19, 'https://picsum.photos/seed/baking_mat1_2/800/800', 0, 2),
(19, 'https://picsum.photos/seed/baking_mat1_3/800/800', 0, 3),

-- SP 20: Cân điện tử
(20, 'https://picsum.photos/seed/baking_scale1_1/800/800', 1, 1),
(20, 'https://picsum.photos/seed/baking_scale1_2/800/800', 0, 2);
GO
