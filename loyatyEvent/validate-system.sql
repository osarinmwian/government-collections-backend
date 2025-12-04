-- System validation script
USE OmniChannelDB2;

PRINT '========================================';
PRINT 'KeyLoyalty System Validation';
PRINT '========================================';

-- Check if all required tables exist
PRINT 'Checking required tables...';
IF EXISTS (SELECT * FROM sysobjects WHERE name='CustomerLoyalty' AND xtype='U')
    PRINT '✓ CustomerLoyalty table exists'
ELSE
    PRINT '✗ CustomerLoyalty table missing';

IF EXISTS (SELECT * FROM sysobjects WHERE name='AccountingEntries' AND xtype='U')
    PRINT '✓ AccountingEntries table exists'
ELSE
    PRINT '✗ AccountingEntries table missing';

IF EXISTS (SELECT * FROM sysobjects WHERE name='KeystoneOmniTransactions' AND xtype='U')
    PRINT '✓ KeystoneOmniTransactions table exists'
ELSE
    PRINT '✗ KeystoneOmniTransactions table missing';

PRINT '';
PRINT 'Table record counts:';
SELECT 'CustomerLoyalty' as TableName, COUNT(*) as Records FROM CustomerLoyalty
UNION ALL
SELECT 'AccountingEntries', COUNT(*) FROM AccountingEntries
UNION ALL  
SELECT 'KeystoneOmniTransactions', COUNT(*) FROM KeystoneOmniTransactions;

PRINT '';
PRINT 'Account mappings:';
SELECT UserId, AccountNumber FROM KeystoneOmniTransactions;

PRINT '';
PRINT 'Current loyalty customers:';
SELECT UserId, TotalPoints, Tier, LastUpdated FROM CustomerLoyalty;

PRINT '';
PRINT 'Recent accounting entries:';
SELECT TOP 10 TransactionId, GLAccount, DebitAmount, CreditAmount, Description, CreatedDate 
FROM AccountingEntries 
ORDER BY CreatedDate DESC;

PRINT '';
PRINT '========================================';
PRINT 'Validation Complete';
PRINT '========================================';