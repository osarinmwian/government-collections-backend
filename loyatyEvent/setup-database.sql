-- Complete database setup for KeyLoyalty system
USE OmniChannelDB2;

-- 1. Create CustomerLoyalty table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CustomerLoyalty' AND xtype='U')
BEGIN
    CREATE TABLE CustomerLoyalty (
        UserId NVARCHAR(50) PRIMARY KEY,
        TotalPoints INT NOT NULL DEFAULT 0,
        Tier INT NOT NULL DEFAULT 1,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'CustomerLoyalty table created';
END

-- 2. Create AccountingEntries table for GL accounting
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

-- 3. Create KeystoneOmniTransactions table for account mapping (if not exists)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='KeystoneOmniTransactions' AND xtype='U')
BEGIN
    CREATE TABLE KeystoneOmniTransactions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        AccountNumber NVARCHAR(50) NOT NULL,
        TransactionType NVARCHAR(50) NULL,
        Amount DECIMAL(18,2) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'KeystoneOmniTransactions table created';
END

-- 4. Insert test data for account mapping
IF NOT EXISTS (SELECT * FROM KeystoneOmniTransactions WHERE AccountNumber = '12338849440')
BEGIN
    INSERT INTO KeystoneOmniTransactions (UserId, AccountNumber, TransactionType, Amount)
    VALUES 
    ('USER123', '12338849440', 'TEST', 0),
    ('USER123', '1234567890', 'TEST', 0),
    ('USER456', '9876543210', 'TEST', 0);
    PRINT 'Test account mapping data inserted';
END

-- 5. Verify tables exist
SELECT 'CustomerLoyalty' as TableName, COUNT(*) as RecordCount FROM CustomerLoyalty
UNION ALL
SELECT 'AccountingEntries', COUNT(*) FROM AccountingEntries  
UNION ALL
SELECT 'KeystoneOmniTransactions', COUNT(*) FROM KeystoneOmniTransactions;

PRINT 'Database setup completed successfully!';