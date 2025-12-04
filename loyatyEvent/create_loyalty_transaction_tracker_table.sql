-- Create table to track loyalty point usage in transactions
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LoyaltyTransactionTracker' AND xtype='U')
BEGIN
    CREATE TABLE LoyaltyTransactionTracker (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TransactionId NVARCHAR(100) NOT NULL UNIQUE,
        AccountNumber NVARCHAR(20) NOT NULL,
        PointsUsed INT NOT NULL,
        AmountUsed DECIMAL(18,2) NOT NULL,
        TransactionType NVARCHAR(50) NOT NULL, -- AIRTIME, TRANSFER, BILL_PAYMENT
        OriginalPoints INT NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING, CONFIRMED, ROLLED_BACK
        RollbackReason NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        ConfirmedDate DATETIME2 NULL,
        RollbackDate DATETIME2 NULL,
        INDEX IX_LoyaltyTransactionTracker_TransactionId (TransactionId),
        INDEX IX_LoyaltyTransactionTracker_AccountNumber (AccountNumber),
        INDEX IX_LoyaltyTransactionTracker_Status (Status)
    );
    
    PRINT 'LoyaltyTransactionTracker table created successfully';
END
ELSE
BEGIN
    PRINT 'LoyaltyTransactionTracker table already exists';
END