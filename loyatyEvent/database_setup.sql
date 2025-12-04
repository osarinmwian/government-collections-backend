-- This file shows the database structure that was removed
-- The loyalty service now reads directly from existing transaction databases:
-- - AirtimeDB: Contains airtime purchase transactions
-- - BillPaymentDB: Contains bill payment transactions  
-- - TransferDB: Contains money transfer transactions
-- - OmniChannelDB2: Main transaction database

-- No loyalty-specific tables are created
-- Loyalty points are calculated from successful transactions in real-time