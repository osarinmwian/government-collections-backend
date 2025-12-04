-- Create ProcessedTransactions table to prevent duplicate point awards
CREATE TABLE ProcessedTransactions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionId NVARCHAR(100) NOT NULL UNIQUE,
    ProcessedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    AccountNumber NVARCHAR(20) NULL,
    Amount DECIMAL(18,2) NULL,
    TransactionType NVARCHAR(50) NULL
);

-- Create index for fast lookups
CREATE INDEX IX_ProcessedTransactions_TransactionId ON ProcessedTransactions(TransactionId);
CREATE INDEX IX_ProcessedTransactions_ProcessedDate ON ProcessedTransactions(ProcessedDate);