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
