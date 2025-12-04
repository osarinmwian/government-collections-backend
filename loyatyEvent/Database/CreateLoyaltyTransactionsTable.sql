-- Create LoyaltyTransactions table to track point usage per transaction
CREATE TABLE LoyaltyTransactions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(50) NOT NULL,
    TransactionReference NVARCHAR(100) NOT NULL,
    PointsUsed INT NOT NULL,
    PointsValue DECIMAL(18,2) NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL, -- 'DEDUCTION', 'AWARD', 'REDEMPTION'
    TransactionAmount DECIMAL(18,2) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    INDEX IX_LoyaltyTransactions_UserId (UserId),
    INDEX IX_LoyaltyTransactions_TransactionRef (TransactionReference),
    INDEX IX_LoyaltyTransactions_CreatedDate (CreatedDate)
);

PRINT 'LoyaltyTransactions table created successfully';