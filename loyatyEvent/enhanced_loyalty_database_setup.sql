-- Enhanced Loyalty System Database Setup
-- This script creates the additional tables needed for the comprehensive loyalty system

USE OmniChannelDB2;
GO

-- Update existing CustomerLoyalty table to include new fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'LifetimePoints')
BEGIN
    ALTER TABLE CustomerLoyalty ADD LifetimePoints INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'IsActive')
BEGIN
    ALTER TABLE CustomerLoyalty ADD IsActive BIT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE CustomerLoyalty ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'TransactionVolume')
BEGIN
    ALTER TABLE CustomerLoyalty ADD TransactionVolume DECIMAL(18,2) NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'TransactionCount')
BEGIN
    ALTER TABLE CustomerLoyalty ADD TransactionCount INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerLoyalty') AND name = 'PointsExpiryDate')
BEGIN
    ALTER TABLE CustomerLoyalty ADD PointsExpiryDate DATETIME2 NOT NULL DEFAULT DATEADD(YEAR, 1, GETUTCDATE());
END

-- Create PointTransactions table for detailed point tracking
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PointTransactions')
BEGIN
    CREATE TABLE PointTransactions (
        Id NVARCHAR(50) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        AccountNumber NVARCHAR(10) NOT NULL,
        Points INT NOT NULL,
        TransactionType NVARCHAR(50) NOT NULL,
        TransactionAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ExpiryDate DATETIME2 NULL,
        IsExpired BIT NOT NULL DEFAULT 0,
        TransactionId NVARCHAR(100) NOT NULL,
        
        INDEX IX_PointTransactions_UserId (UserId),
        INDEX IX_PointTransactions_AccountNumber (AccountNumber),
        INDEX IX_PointTransactions_CreatedDate (CreatedDate),
        INDEX IX_PointTransactions_ExpiryDate (ExpiryDate),
        INDEX IX_PointTransactions_TransactionType (TransactionType)
    );
    
    PRINT 'PointTransactions table created successfully';
END

-- Create PointRedemptions table for redemption tracking
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PointRedemptions')
BEGIN
    CREATE TABLE PointRedemptions (
        Id NVARCHAR(50) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        AccountNumber NVARCHAR(10) NOT NULL,
        PointsRedeemed INT NOT NULL,
        AmountRedeemed DECIMAL(18,2) NOT NULL,
        RedemptionType INT NOT NULL, -- 0=CASHBACK, 1=DISCOUNT, 2=VOUCHER, 3=TRANSFER
        Description NVARCHAR(500) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        TransactionId NVARCHAR(100) NOT NULL,
        IsSuccessful BIT NOT NULL DEFAULT 1,
        
        INDEX IX_PointRedemptions_UserId (UserId),
        INDEX IX_PointRedemptions_AccountNumber (AccountNumber),
        INDEX IX_PointRedemptions_CreatedDate (CreatedDate),
        INDEX IX_PointRedemptions_RedemptionType (RedemptionType)
    );
    
    PRINT 'PointRedemptions table created successfully';
END

-- Create LoyaltyAlerts table for customer notifications
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyAlerts')
BEGIN
    CREATE TABLE LoyaltyAlerts (
        Id NVARCHAR(50) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        AccountNumber NVARCHAR(10) NOT NULL,
        AlertType NVARCHAR(50) NOT NULL, -- EARNING, REDEMPTION, EXPIRY, TIER_UPGRADE
        Message NVARCHAR(1000) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsRead BIT NOT NULL DEFAULT 0,
        
        INDEX IX_LoyaltyAlerts_UserId (UserId),
        INDEX IX_LoyaltyAlerts_AccountNumber (AccountNumber),
        INDEX IX_LoyaltyAlerts_CreatedDate (CreatedDate),
        INDEX IX_LoyaltyAlerts_IsRead (IsRead),
        INDEX IX_LoyaltyAlerts_AlertType (AlertType)
    );
    
    PRINT 'LoyaltyAlerts table created successfully';
END

-- Create LoyaltyConfiguration table for system settings
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyConfiguration')
BEGIN
    CREATE TABLE LoyaltyConfiguration (
        ConfigKey NVARCHAR(100) PRIMARY KEY,
        ConfigValue NVARCHAR(500) NOT NULL,
        Description NVARCHAR(1000) NULL,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL
    );
    
    -- Insert default configuration values
    INSERT INTO LoyaltyConfiguration (ConfigKey, ConfigValue, Description) VALUES
    ('POINTS_PER_NAIRA_TRANSFER', '2', 'Points earned per ₦1000 transfer transaction'),
    ('POINTS_PER_NAIRA_AIRTIME', '1', 'Points earned per ₦1000 airtime transaction'),
    ('POINTS_PER_NAIRA_BILL', '3', 'Points earned per ₦1000 bill payment transaction'),
    ('POINT_VALUE_NAIRA', '1.00', 'Value of 1 point in Naira'),
    ('POINTS_EXPIRY_MONTHS', '12', 'Number of months before points expire'),
    ('MAX_DAILY_POINTS', '100', 'Maximum points that can be earned per day'),
    ('MAX_DAILY_REDEMPTIONS', '5', 'Maximum number of redemptions per day'),
    ('MAX_DAILY_REDEMPTION_AMOUNT', '50000', 'Maximum redemption amount per day in Naira'),
    ('BRONZE_TIER_MIN', '0', 'Minimum points for Bronze tier'),
    ('SILVER_TIER_MIN', '501', 'Minimum points for Silver tier'),
    ('GOLD_TIER_MIN', '3001', 'Minimum points for Gold tier'),
    ('DIAMOND_TIER_MIN', '6001', 'Minimum points for Diamond tier'),
    ('PLATINUM_TIER_MIN', '10001', 'Minimum points for Platinum tier');
    
    PRINT 'LoyaltyConfiguration table created and populated successfully';
END

-- Create stored procedures for common operations

-- Procedure to get customer loyalty summary
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetCustomerLoyaltySummary')
    DROP PROCEDURE sp_GetCustomerLoyaltySummary;
GO

CREATE PROCEDURE sp_GetCustomerLoyaltySummary
    @UserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        cl.UserId,
        cl.TotalPoints,
        cl.LifetimePoints,
        cl.Tier,
        cl.TransactionVolume,
        cl.TransactionCount,
        cl.PointsExpiryDate,
        cl.IsActive,
        cl.CreatedDate,
        cl.LastUpdated,
        
        -- Recent earnings (last 30 days)
        ISNULL(recent_earnings.EarningsCount, 0) as RecentEarningsCount,
        ISNULL(recent_earnings.EarningsPoints, 0) as RecentEarningsPoints,
        
        -- Recent redemptions (last 30 days)
        ISNULL(recent_redemptions.RedemptionsCount, 0) as RecentRedemptionsCount,
        ISNULL(recent_redemptions.RedemptionsPoints, 0) as RecentRedemptionsPoints,
        
        -- Expiring points (next 30 days)
        ISNULL(expiring_points.ExpiringPoints, 0) as ExpiringPointsNext30Days,
        
        -- Unread alerts count
        ISNULL(unread_alerts.UnreadCount, 0) as UnreadAlertsCount
        
    FROM CustomerLoyalty cl
    
    LEFT JOIN (
        SELECT UserId, COUNT(*) as EarningsCount, SUM(Points) as EarningsPoints
        FROM PointTransactions 
        WHERE UserId = @UserId AND Points > 0 AND CreatedDate >= DATEADD(DAY, -30, GETUTCDATE())
        GROUP BY UserId
    ) recent_earnings ON cl.UserId = recent_earnings.UserId
    
    LEFT JOIN (
        SELECT UserId, COUNT(*) as RedemptionsCount, SUM(PointsRedeemed) as RedemptionsPoints
        FROM PointRedemptions 
        WHERE UserId = @UserId AND CreatedDate >= DATEADD(DAY, -30, GETUTCDATE())
        GROUP BY UserId
    ) recent_redemptions ON cl.UserId = recent_redemptions.UserId
    
    LEFT JOIN (
        SELECT UserId, SUM(Points) as ExpiringPoints
        FROM PointTransactions 
        WHERE UserId = @UserId AND ExpiryDate <= DATEADD(DAY, 30, GETUTCDATE()) 
              AND IsExpired = 0 AND Points > 0
        GROUP BY UserId
    ) expiring_points ON cl.UserId = expiring_points.UserId
    
    LEFT JOIN (
        SELECT UserId, COUNT(*) as UnreadCount
        FROM LoyaltyAlerts 
        WHERE UserId = @UserId AND IsRead = 0
        GROUP BY UserId
    ) unread_alerts ON cl.UserId = unread_alerts.UserId
    
    WHERE cl.UserId = @UserId;
END
GO

-- Procedure to expire points
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_ExpirePoints')
    DROP PROCEDURE sp_ExpirePoints;
GO

CREATE PROCEDURE sp_ExpirePoints
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ExpiredPoints TABLE (
        UserId NVARCHAR(50),
        TotalExpiredPoints INT
    );
    
    -- Mark expired points
    UPDATE PointTransactions 
    SET IsExpired = 1
    OUTPUT DELETED.UserId, DELETED.Points INTO @ExpiredPoints
    WHERE ExpiryDate <= GETUTCDATE() AND IsExpired = 0 AND Points > 0;
    
    -- Update customer total points
    UPDATE cl
    SET TotalPoints = cl.TotalPoints - ep.TotalExpiredPoints,
        LastUpdated = GETUTCDATE()
    FROM CustomerLoyalty cl
    INNER JOIN (
        SELECT UserId, SUM(TotalExpiredPoints) as TotalExpiredPoints
        FROM @ExpiredPoints
        GROUP BY UserId
    ) ep ON cl.UserId = ep.UserId;
    
    -- Create expiry alerts
    INSERT INTO LoyaltyAlerts (Id, UserId, AccountNumber, AlertType, Message, CreatedDate, IsRead)
    SELECT 
        NEWID(),
        ep.UserId,
        '', -- AccountNumber would need to be resolved
        'EXPIRY',
        CONCAT(ep.TotalExpiredPoints, ' points have expired due to inactivity'),
        GETUTCDATE(),
        0
    FROM (
        SELECT UserId, SUM(TotalExpiredPoints) as TotalExpiredPoints
        FROM @ExpiredPoints
        GROUP BY UserId
    ) ep;
    
    SELECT COUNT(*) as ProcessedUsers, SUM(TotalExpiredPoints) as TotalExpiredPoints
    FROM (
        SELECT UserId, SUM(TotalExpiredPoints) as TotalExpiredPoints
        FROM @ExpiredPoints
        GROUP BY UserId
    ) summary;
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerLoyalty_Tier')
    CREATE INDEX IX_CustomerLoyalty_Tier ON CustomerLoyalty (Tier);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerLoyalty_TotalPoints')
    CREATE INDEX IX_CustomerLoyalty_TotalPoints ON CustomerLoyalty (TotalPoints);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomerLoyalty_LastUpdated')
    CREATE INDEX IX_CustomerLoyalty_LastUpdated ON CustomerLoyalty (LastUpdated);

-- Create a view for loyalty analytics
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_LoyaltyAnalytics')
    DROP VIEW vw_LoyaltyAnalytics;
GO

CREATE VIEW vw_LoyaltyAnalytics AS
SELECT 
    cl.Tier,
    COUNT(*) as CustomerCount,
    AVG(CAST(cl.TotalPoints as FLOAT)) as AvgPoints,
    SUM(cl.TotalPoints) as TotalPoints,
    AVG(CAST(cl.TransactionVolume as FLOAT)) as AvgTransactionVolume,
    SUM(cl.TransactionVolume) as TotalTransactionVolume,
    AVG(CAST(cl.TransactionCount as FLOAT)) as AvgTransactionCount,
    SUM(cl.TransactionCount) as TotalTransactionCount
FROM CustomerLoyalty cl
WHERE cl.IsActive = 1
GROUP BY cl.Tier;
GO

PRINT 'Enhanced Loyalty System database setup completed successfully!';
PRINT 'Tables created: PointTransactions, PointRedemptions, LoyaltyAlerts, LoyaltyConfiguration';
PRINT 'Stored procedures created: sp_GetCustomerLoyaltySummary, sp_ExpirePoints';
PRINT 'View created: vw_LoyaltyAnalytics';
PRINT 'Indexes and configuration data added successfully';