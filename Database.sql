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

-- DANH MỤC 1: BỘT CÁC LOẠI (ID 1-20)
(1, 1, 'B-MDD-01', N'Bột Mì Đa Dụng Hoa Hồng (1kg)', N'Bột mì cơ bản cho nhiều loại bánh.', N'Gói', 1.00, 22000, 150, 0),
(2, 1, 'B-MDD-02', N'Bột Mì Đa Dụng Meizan (1kg)', N'Bột mì thông dụng, dễ sử dụng.', N'Gói', 1.00, 21000, 200, 0),
(3, 1, 'B-S11-01', N'Bột Số 11 (Cái Cân) (1kg)', N'Chuyên dụng làm bánh mì.', N'Gói', 1.00, 25000, 120, 0),
(4, 1, 'B-S11-02', N'Bột Bánh Mì Baker Choice (1kg)', N'Protein cao, cho thớ bánh dai ngon.', N'Gói', 1.00, 27000, 100, 0),
(5, 1, 'B-S08-01', N'Bột Số 8 (Táo Đỏ) (1kg)', N'Chuyên làm bánh bông lan xốp mềm.', N'Gói', 1.00, 26000, 110, 0),
(6, 1, 'B-S08-02', N'Bột Bánh Ngọt Baker Choice (1kg)', N'Bột mịn, nhẹ cho bánh xốp.', N'Gói', 1.00, 28000, 90, 0),
(7, 1, 'B-HAN-01', N'Bột Hạnh Nhân Nguyên Chất (500g)', N'Làm Macaron, bánh quy hạt.', N'Gói', 0.50, 185000, 50, 0),
(8, 1, 'B-HAN-02', N'Bột Hạnh Nhân Mỹ Siêu Mịn (250g)', N'Hạt mịn không lợn cợn.', N'Gói', 0.25, 95000, 60, 0),
(9, 1, 'B-BAP-01', N'Bột Bắp (Ngô) (500g)', N'Giúp bánh mềm, tạo độ sánh.', N'Gói', 0.50, 15000, 200, 0),
(10,1, 'B-NAN-01', N'Bột Năng (500g)', N'Tạo độ dai cho trân châu, bánh bột.', N'Gói', 0.50, 14000, 180, 0),
(11,1, 'B-NEP-01', N'Bột Nếp Thái Tài (500g)', N'Làm bánh dẻo, mochi.', N'Gói', 0.50, 18000, 100, 0),
(12,1, 'B-GAO-01', N'Bột Gạo Tẻ (500g)', N'Làm bánh cuốn, bánh đúc.', N'Gói', 0.50, 15000, 150, 0),
(13,1, 'B-TMI-01', N'Bột Tàn Mì (Wheat Starch) (500g)', N'Làm vỏ há cảo trong suốt.', N'Gói', 0.50, 25000, 80, 0),
(14,1, 'B-YEM-01', N'Bột Yến Mạch Quaker (500g)', N'Làm bánh healthy, ăn kiêng.', N'Gói', 0.50, 45000, 100, 0),
(15,1, 'B-NCA-01', N'Bột Mì Nguyên Cám (1kg)', N'Nhiều chất xơ, làm bánh mì đen.', N'Gói', 1.00, 55000, 70, 0),
(16,1, 'B-RYE-01', N'Bột Lúa Mạch Đen Rye (1kg)', N'Làm bánh mì chua Sourdough.', N'Gói', 1.00, 65000, 50, 0),
(17,1, 'B-DUA-01', N'Bột Dừa (250g)', N'Phù hợp chế độ ăn Keto.', N'Gói', 0.25, 45000, 60, 0),
(18,1, 'B-TTR-01', N'Bột Trà Xanh Đài Loan (100g)', N'Tạo màu và mùi vị thơm mát.', N'Gói', 0.10, 35000, 150, 0),
(19,1, 'B-MAT-01', N'Bột Matcha Nhật Bản (50g)', N'Matcha cao cấp làm bánh và đồ uống.', N'Gói', 0.05, 120000, 40, 0),
(20,1, 'B-CCA-01', N'Bột Cacao Nguyên Chất (250g)', N'Làm brownie, truffle đậm vị.', N'Gói', 0.25, 65000, 100, 0),

-- DANH MỤC 2: BƠ, SỮA & PHÔ MAI (ID 21-40)
(21, 2, 'BO-ANC-250', N'Bơ Lạt Anchor (250g)', N'Bơ động vật nguyên chất từ New Zealand.', N'Khối', 0.25, 65000, 80, 0),
(22, 2, 'BO-ANC-1KG', N'Bơ Lạt Anchor (1kg)', N'Tiết kiệm cho tiệm bánh lớn.', N'Khối', 1.00, 23000, 40, 0),
(23, 2, 'BO-PRE-200', N'Bơ Lạt President (200g)', N'Bơ Pháp thượng hạng.', N'Khối', 0.20, 85000, 50, 0),
(24, 2, 'BO-TH-200',  N'Bơ Lạt TH True Milk (200g)', N'Bơ lạt sản xuất tại Việt Nam.', N'Khối', 0.20, 55000, 90, 0),
(25, 2, 'BO-MAN-250', N'Bơ Mặn Anchor (250g)', N'Dùng để nấu ăn hoặc làm bánh mặn.', N'Khối', 0.25, 66000, 60, 0),
(26, 2, 'WH-ANC-1L',  N'Whipping Cream Anchor (1L)', N'Kem tươi chà láng bánh kem.', N'Hộp', 1.00, 145000, 60, 0),
(27, 2, 'WH-TAT-1L',  N'Whipping Cream Tatua (1L)', N'Độ béo 36%, dễ đánh bông.', N'Hộp', 1.00, 135000, 70, 0),
(28, 2, 'WH-ELL-1L',  N'Whipping Cream Elle & Vire (1L)', N'Kem tươi Pháp cao cấp.', N'Hộp', 1.00, 165000, 40, 0),
(29, 2, 'TP-GOL-1L',  N'Topping Cream Gold Label (1L)', N'Kem thực vật, chịu nhiệt tốt.', N'Hộp', 1.00, 85000, 100, 0),
(30, 2, 'TP-SIL-1L',  N'Topping Cream Silver (1L)', N'Phù hợp trang trí bánh kem.', N'Hộp', 1.00, 75000, 90, 0),
(31, 2, 'CC-PHI-250', N'Cream Cheese Philadelphia (250g)', N'Phô mai kem chuẩn vị Tiramisu.', N'Hộp', 0.25, 95000, 50, 0),
(32, 2, 'CC-PHI-1KG', N'Cream Cheese Philadelphia (1kg)', N'Size lớn tiết kiệm.', N'Hộp', 1.00, 320000, 20, 0),
(33, 2, 'CC-ANC-1KG', N'Cream Cheese Anchor (1kg)', N'Phô mai kem vị chua nhẹ.', N'Hộp', 1.00, 250000, 30, 0),
(34, 2, 'MAS-TAT-500',N'Phô Mai Mascarpone Tatua (500g)', N'Độ béo ngậy cao, làm cheesecake.', N'Hộp', 0.50, 120000, 40, 0),
(35, 2, 'MOZ-KHO-500',N'Phô Mai Mozzarella Khối (500g)', N'Làm pizza, bánh phô mai nướng.', N'Khối', 0.50, 110000, 60, 0),
(36, 2, 'MOZ-BAO-500',N'Phô Mai Mozzarella Bào (500g)', N'Tiện dụng, dễ tan chảy.', N'Gói', 0.50, 120000, 80, 0),
(37, 2, 'SU-TH-1L',   N'Sữa Tươi Không Đường TH (1L)', N'Sữa tươi cơ bản cho mọi công thức.', N'Hộp', 1.00, 36000, 200, 0),
(38, 2, 'SU-VIN-1L',  N'Sữa Tươi Có Đường Vinamilk (1L)', N'Dùng pha chế, làm bánh béo ngọt.', N'Hộp', 1.00, 35000, 150, 0),
(39, 2, 'SU-OGT-380', N'Sữa Đặc Ông Thọ (380g)', N'Làm flan, pha đồ uống.', N'Lon', 0.38, 25000, 300, 0),
(40, 2, 'SU-BOT-500', N'Sữa Bột Nguyên Kem Nzmp (500g)', N'Tăng hương vị sữa cho bánh quy.', N'Gói', 0.50, 95000, 70, 0),

-- DANH MỤC 3: ĐƯỜNG & HƯƠNG LIỆU (ID 41-60)
(41, 3, 'D-CAT-1KG',  N'Đường Cát Trắng (1kg)', N'Đường tinh luyện thông dụng.', N'Gói', 1.00, 25000, 250, 0),
(42, 3, 'D-ICI-500',  N'Đường Bột Icing (500g)', N'Siêu mịn, làm kem bơ.', N'Gói', 0.50, 25000, 100, 0),
(43, 3, 'D-DEN-1KG',  N'Đường Đen Hàn Quốc (1kg)', N'Làm trân châu đường đen.', N'Gói', 1.00, 65000, 80, 0),
(44, 3, 'D-NAU-1KG',  N'Đường Nâu (1kg)', N'Tạo màu đẹp cho bánh quy, cookie.', N'Gói', 1.00, 35000, 90, 0),
(45, 3, 'D-PHE-500',  N'Đường Phèn Hạt Nhỏ (500g)', N'Ngọt thanh, nấu chè, làm thạch.', N'Gói', 0.50, 28000, 60, 0),
(46, 3, 'VA-RAY-28',  N'Tinh Chất Vani Rayner (28ml)', N'Khử mùi tanh của trứng.', N'Chai', 0.05, 35000, 150, 0),
(47, 3, 'VA-BOT-100', N'Vani Bột (100g)', N'Tiện dụng, bảo quản lâu.', N'Hộp', 0.10, 20000, 200, 0),
(48, 3, 'VA-WIL-50',  N'Chiết Xuất Vani Wilton (50ml)', N'Vani cao cấp dạng lỏng.', N'Chai', 0.05, 120000, 30, 0),
(49, 3, 'BP-RUM-114', N'Bột Nở Baking Powder Rumford (114g)', N'Không chứa nhôm, an toàn.', N'Hộp', 0.11, 55000, 90, 0),
(50, 3, 'BP-MAU-50',  N'Bột Nở Mauri (50g)', N'Gói nhỏ gọn cho gia đình.', N'Gói', 0.05, 15000, 150, 0),
(51, 3, 'BS-ARM-454', N'Muối Nở Baking Soda Arm & Hammer (454g)',N'Giúp xốp bánh, vệ sinh bếp.', N'Hộp', 0.45, 45000, 120, 0),
(52, 3, 'YE-MAU-500', N'Men Nở Mauri Lộc Đỏ (500g)', N'Men khô lạt làm bánh mì.', N'Gói', 0.50, 65000, 70, 0),
(53, 3, 'YE-SAF-500', N'Men Nở Saf-Instant Vàng (500g)', N'Men Pháp cho bánh mì ngọt.', N'Gói', 0.50, 85000, 60, 0),
(54, 3, 'GE-LA-10',   N'Gelatin Lá Ewald (10 lá)', N'Làm thạch, mousse đông đặc.', N'Gói', 0.05, 35000, 100, 0),
(55, 3, 'GE-BOT-100', N'Gelatin Bột Bloom 250 (100g)', N'Dễ dàng hòa tan.', N'Gói', 0.10, 45000, 80, 0),
(56, 3, 'SI-BAP-500', N'Siro Bắp Corn Syrup (500ml)', N'Ngăn đường kết tinh, tạo bóng.', N'Chai', 0.50, 55000, 50, 0),
(57, 3, 'MA-ONG-500', N'Mật Ong Rừng Nguyên Chất (500ml)', N'Tạo hương vị tự nhiên.', N'Chai', 0.50, 150000, 40, 0),
(58, 3, 'CO-RED-20',  N'Màu Thực Phẩm Đỏ (20ml)', N'Màu dạng nước an toàn.', N'Chai', 0.02, 25000, 100, 0),
(59, 3, 'CO-YEL-20',  N'Màu Thực Phẩm Vàng (20ml)', N'Màu thực phẩm cơ bản.', N'Chai', 0.02, 25000, 100, 0),
(60, 3, 'HU-DUA-20',  N'Hương Lá Dứa (20ml)', N'Hương thơm tự nhiên.', N'Chai', 0.02, 22000, 90, 0),

-- DANH MỤC 4: SÔ CÔ LA & HẠT, TRÁI CÂY (ID 61-80)
(61, 4, 'CH-D65-1KG', N'Socola Đen 65% Puratos (1kg)', N'Độ đắng vừa, hạt nút dễ chảy.', N'Gói', 1.00, 195000, 45, 0),
(62, 4, 'CH-D75-1KG', N'Socola Đen 75% Grand Place (1kg)', N'Đắng đậm cho tín đồ dark choco.', N'Gói', 1.00, 215000, 30, 0),
(63, 4, 'CH-WHT-1KG', N'Socola Trắng Nút Cúc (1kg)', N'Vị sữa ngọt ngào.', N'Gói', 1.00, 185000, 50, 0),
(64, 4, 'CH-MIL-1KG', N'Socola Sữa (1kg)', N'Socola sữa ngọt, dễ ăn.', N'Gói', 1.00, 180000, 40, 0),
(65, 4, 'CH-CHP-500', N'Socola Chip Đen (500g)', N'Giữ nguyên hình dạng khi nướng.', N'Gói', 0.50, 75000, 80, 0),
(66, 4, 'CH-CHW-500', N'Socola Chip Trắng (500g)', N'Chip trắng trang trí.', N'Gói', 0.50, 80000, 70, 0),
(67, 4, 'HA-LAT-250', N'Hạnh Nhân Lát (250g)', N'Làm bánh ngói hạnh nhân.', N'Gói', 0.25, 85000, 65, 0),
(68, 4, 'HA-HAT-500', N'Hạnh Nhân Nguyên Hạt (500g)', N'Hạt nướng thơm lừng.', N'Gói', 0.50, 165000, 40, 0),
(69, 4, 'HA-OC-250',  N'Hạt Óc Chó Mỹ (250g)', N'Tốt cho sức khỏe, làm bánh quy.', N'Gói', 0.25, 110000, 50, 0),
(70, 4, 'HA-CU-250',  N'Hạt Dẻ Cười Bóc Vỏ (250g)', N'Hạt dẻ xanh trang trí cao cấp.', N'Gói', 0.25, 180000, 30, 0),
(71, 4, 'HA-DI-500',  N'Hạt Điều Rang Tách Đôi (500g)', N'Giòn, bùi, làm kẹo hạt.', N'Gói', 0.50, 120000, 60, 0),
(72, 4, 'HA-DU-500',  N'Hạt Dưa Bóc Vỏ (500g)', N'Làm bánh trung thu.', N'Gói', 0.50, 95000, 70, 0),
(73, 4, 'HA-BI-500',  N'Hạt Bí Xanh Ấn Độ (500g)', N'Làm kẹo nougat chà là.', N'Gói', 0.50, 115000, 80, 0),
(74, 4, 'TR-NHO-500', N'Nho Khô Vàng Mỹ (500g)', N'Trái to, ngọt thanh.', N'Gói', 0.50, 95000, 90, 0),
(75, 4, 'TR-NHOD-500',N'Nho Khô Đen (500g)', N'Nho khô nguyên vị.', N'Gói', 0.50, 85000, 85, 0),
(76, 4, 'TR-VQ-250',  N'Nam Việt Quất Khô (Cranberry) (250g)', N'Chua ngọt tự nhiên.', N'Gói', 0.25, 95000, 60, 0),
(77, 4, 'TR-CLA-500', N'Chà Là Không Hạt (500g)', N'Mềm, ngọt, thay thế đường.', N'Gói', 0.50, 85000, 70, 0),
(78, 4, 'MU-DAU-300', N'Mứt Dâu Tây Golden Farm (300g)', N'Lấp đầy nhân bánh.', N'Hũ', 0.30, 45000, 60, 0),
(79, 4, 'MU-VQ-300',  N'Mứt Việt Quất (300g)', N'Nhân bánh thơm ngon.', N'Hũ', 0.30, 55000, 50, 0),
(80, 4, 'MU-CAM-300', N'Mứt Cam Marmalade (300g)', N'Có vỏ cam thơm lừng.', N'Hũ', 0.30, 45000, 55, 0),

-- DANH MỤC 5: DỤNG CỤ LÀM BÁNH (ID 81-100)
(81, 5, 'DC-PL-25',   N'Phới Lồng Đánh Trứng Inox 25cm', N'Cầm chắc tay, siêu bền.', N'Cái', 0.15, 45000, 120, 0),
(82, 5, 'DC-PL-30',   N'Phới Lồng Inox 30cm', N'Dành cho tô trộn lớn.', N'Cái', 0.20, 55000, 90, 0),
(83, 5, 'DC-SP-21',   N'Phới Dẹt Spatula Silicon 21cm', N'Vét sạch âu trộn bột.', N'Cái', 0.10, 25000, 200, 0),
(84, 5, 'DC-SP-28',   N'Phới Dẹt Spatula Silicon 28cm', N'Silicon chịu nhiệt cao.', N'Cái', 0.15, 35000, 150, 0),
(85, 5, 'DC-KTR-16',  N'Khuôn Tròn Đế Rời 16cm', N'Khuôn nướng bánh xốp.', N'Cái', 0.20, 65000, 80, 0),
(86, 5, 'DC-KTR-20',  N'Khuôn Tròn Đế Rời 20cm', N'Khuôn chống dính size chuẩn.', N'Cái', 0.30, 85000, 70, 0),
(87, 5, 'DC-KVU-20',  N'Khuôn Vuông Đế Liền 20x20cm', N'Dùng làm bánh brownie.', N'Cái', 0.35, 90000, 50, 0),
(88, 5, 'DC-CUP-12',  N'Khay Cupcake 12 Ô Chống Dính', N'Nướng muffin, cupcake.', N'Cái', 0.50, 110000, 40, 0),
(89, 5, 'DC-TNB-60',  N'Tấm Nhào Bột Silicon (60x40cm)', N'Có vạch chia kích thước.', N'Cái', 0.20, 95000, 100, 0),
(90, 5, 'DC-CAN-5',   N'Cân Điện Tử Tiểu Ly (Max 5kg)', N'Độ chính xác 1 gram.', N'Cái', 0.40, 110000, 60, 0),
(91, 5, 'DC-NKL-1',   N'Nhiệt Kế Lò Nướng', N'Kiểm soát nhiệt độ lò chính xác.', N'Cái', 0.10, 85000, 80, 0),
(92, 5, 'DC-GNEN-10', N'Giấy Nến Chống Dính (Cuộn 10m)', N'Lót khay nướng.', N'Cuộn', 0.20, 35000, 150, 0),
(93, 5, 'DC-CLAN-30', N'Cán Lăn Bột Gỗ Sồi 30cm', N'Cán mỏng đế bánh tart, pizza.', N'Cái', 0.30, 45000, 100, 0),
(94, 5, 'DC-TBK-100', N'Túi Bắt Kem Nilon (100 chiếc)', N'Túi xài một lần tiện dụng.', N'Bịch', 0.20, 35000, 200, 0),
(95, 5, 'DC-DBK-24',  N'Bộ Đuôi Bắt Kem Inox 24 Cái', N'Tạo đủ loại hoa, họa tiết.', N'Hộp', 0.25, 85000, 50, 0),
(96, 5, 'DC-BX-1',    N'Bàn Xoay Chà Láng Bánh Kem', N'Nhựa ABS mượt mà.', N'Cái', 0.50, 95000, 40, 0),
(97, 5, 'DC-DCL-8',   N'Dao Chà Láng Gấp Khúc 8 inch', N'Chà phẳng mặt kem.', N'Cái', 0.15, 40000, 80, 0),
(98, 5, 'DC-CRQ-1',   N'Cây Cắt Ráp Quét Bột Bán Nguyệt', N'Nhựa an toàn, vét âu sạch.', N'Cái', 0.05, 15000, 250, 0),
(99, 5, 'DC-CT-10',   N'Bộ Cốc Thìa Đong 10 Món', N'Đo lường ml và cup chuẩn.', N'Bộ', 0.20, 55000, 90, 0),
(100,5, 'DC-MDT-1',   N'Máy Đánh Trứng Cầm Tay Bear', N'Công suất 300W mạnh mẽ.', N'Cái', 1.50, 350000, 30, 0);
SET IDENTITY_INSERT Product OFF;
GO

INSERT INTO ProductImage (ProductId, ImageUrl, IsPrimary, SortOrder)
SELECT 
    ProductId,
    'https://picsum.photos/seed/GHP_Main_' + CAST(ProductId AS VARCHAR) + '/800/800',
    1,
    1
FROM Product;
GO

-- 3. Tự động sinh Ảnh Phụ (IsPrimary = 0) siêu ổn định từ Picsum
INSERT INTO ProductImage (ProductId, ImageUrl, IsPrimary, SortOrder)
SELECT 
    ProductId,
    'https://picsum.photos/seed/GHP_Sub_' + CAST(ProductId AS VARCHAR) + '/800/800',
    0,
    2
FROM Product;
GO