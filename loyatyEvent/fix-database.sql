-- Fix database setup for KeyLoyalty system
USE OmniChannelDB2;

-- Drop and recreate CustomerLoyalty table with correct structure
IF EXISTS (SELECT * FROM sysobjects WHERE name='CustomerLoyalty' AND xtype='U')
BEGIN
    DROP TABLE CustomerLoyalty;
    PRINT 'Dropped existing CustomerLoyalty table';
END

-- Create CustomerLoyalty table
CREATE TABLE CustomerLoyalty (
    UserId NVARCHAR(50) PRIMARY KEY,
    TotalPoints INT NOT NULL DEFAULT 0,
    Tier INT NOT NULL DEFAULT 1,
    LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
PRINT 'CustomerLoyalty table created successfully';

-- Create AccountingEntries table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AccountingEntries' AND xtype='U')
BEGIN
    CREATE TABLE AccountingEntries (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TransactionId NVARCHAR(50) NOT NULL,
        GLAccount NVARCHAR(50) NOT NULL,
        DebitAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreditAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        AccountNumber NVARCHAR(50) NULL
    );
    PRINT 'AccountingEntries table created';
END

-- Verify table structure
SELECT 'CustomerLoyalty' as TableName, COUNT(*) as RecordCount FROM CustomerLoyalty;
SELECT 'AccountingEntries' as TableName, COUNT(*) as RecordCount FROM AccountingEntries;

PRINT 'Database setup completed successfully!';