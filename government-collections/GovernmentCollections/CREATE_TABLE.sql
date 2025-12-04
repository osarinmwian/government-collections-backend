CREATE TABLE GovernmentPayments (
    Id int IDENTITY(1,1) PRIMARY KEY,
    TransactionReference nvarchar(100) NOT NULL UNIQUE,
    CustomerReference nvarchar(100) NOT NULL,
    PayerName nvarchar(200) NOT NULL,
    PayerEmail nvarchar(100) NOT NULL,
    PayerPhone nvarchar(20) NOT NULL,
    PaymentType int NOT NULL,
    Gateway int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Description nvarchar(500) NOT NULL,
    Status int NOT NULL,
    GatewayReference nvarchar(100) NOT NULL DEFAULT '',
    GatewayResponse nvarchar(max) NOT NULL DEFAULT '',
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NULL,
    Channel nvarchar(50) NOT NULL DEFAULT '',
    UserId nvarchar(100) NOT NULL
);

CREATE INDEX IX_GovernmentPayments_UserId ON GovernmentPayments(UserId);
CREATE INDEX IX_GovernmentPayments_Status ON GovernmentPayments(Status);
CREATE INDEX IX_GovernmentPayments_CreatedAt ON GovernmentPayments(CreatedAt);