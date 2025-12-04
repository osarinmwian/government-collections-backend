-- Create table to track processed transactions to prevent duplicate point assignments
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessedTransactions' AND xtype='U')
BEGIN
    CREATE TABLE ProcessedTransactions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TransactionId NVARCHAR(100) NOT NULL UNIQUE,
        ProcessedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        INDEX IX_ProcessedTransactions_TransactionId (TransactionId),
        INDEX IX_ProcessedTransactions_ProcessedDate (ProcessedDate)
    );
    
    PRINT 'ProcessedTransactions table created successfully';
END
ELSE
BEGIN
    PRINT 'ProcessedTransactions table already exists';
END

-- Clean up old processed transactions (older than 30 days) to prevent table growth
DELETE FROM ProcessedTransactions 
WHERE ProcessedDate < DATEADD(DAY, -30, GETDATE());

PRINT 'Old processed transactions cleaned up';