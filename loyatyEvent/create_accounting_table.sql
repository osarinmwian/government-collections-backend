-- Create AccountingEntries table for GL accounting
USE OmniChannelDB2;

-- Create the table if it doesn't exist
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
    
    PRINT 'AccountingEntries table created successfully';
END
ELSE
BEGIN
    PRINT 'AccountingEntries table already exists';
END