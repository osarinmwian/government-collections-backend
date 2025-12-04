-- Create PendingRedemptions table to track loyalty point usage before transaction completion
CREATE TABLE PendingRedemptions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RedemptionId NVARCHAR(100) NOT NULL UNIQUE,
    UserId NVARCHAR(50) NOT NULL,
    Points INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    TransactionRef NVARCHAR(100) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING, CONFIRMED, FAILED
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ConfirmedDate DATETIME2 NULL
);

-- Create indexes
CREATE INDEX IX_PendingRedemptions_RedemptionId ON PendingRedemptions(RedemptionId);
CREATE INDEX IX_PendingRedemptions_UserId ON PendingRedemptions(UserId);
CREATE INDEX IX_PendingRedemptions_Status ON PendingRedemptions(Status);