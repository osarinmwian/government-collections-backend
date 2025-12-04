-- Add PointsExpiryDate column to CustomerLoyalty table
-- Run this script on your database to enable point expiry functionality

USE [OmniChannelDB2]
GO

-- Check if column already exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CustomerLoyalty]') AND name = 'PointsExpiryDate')
BEGIN
    -- Add the PointsExpiryDate column
    ALTER TABLE [dbo].[CustomerLoyalty]
    ADD [PointsExpiryDate] DATETIME2 NOT NULL DEFAULT DATEADD(YEAR, 1, GETDATE())
    
    PRINT 'PointsExpiryDate column added successfully to CustomerLoyalty table'
END
ELSE
BEGIN
    PRINT 'PointsExpiryDate column already exists in CustomerLoyalty table'
END
GO

-- Update existing records to have expiry date 1 year from now
UPDATE [dbo].[CustomerLoyalty] 
SET [PointsExpiryDate] = DATEADD(YEAR, 1, GETDATE())
WHERE [PointsExpiryDate] IS NULL OR [PointsExpiryDate] = '1900-01-01'
GO

PRINT 'Database migration completed successfully'