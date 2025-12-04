# KeyLoyalty Database Setup Guide

## Overview
This guide provides step-by-step instructions for setting up the database for the KeyLoyalty Event-Driven System.

## Prerequisites
- SQL Server instance running on `10.40.14.22:1433`
- SQL Server credentials: `DevSol` / `DevvSol1234`
- `sqlcmd` utility installed on your machine
- .NET 9.0 SDK installed

## Database Setup Steps

### Step 1: Create the KeyLoyalty Database

```sql
-- Connect to SQL Server and create the database
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "CREATE DATABASE KeyLoyaltyDB"
```

### Step 2: Create the CustomerLoyalty Table

```sql
-- Create the main loyalty table
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "
CREATE TABLE CustomerLoyalty (
    AccountNumber NVARCHAR(50) PRIMARY KEY,
    TotalPoints INT NOT NULL DEFAULT 0,
    Tier INT NOT NULL DEFAULT 1,
    LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)"
```

### Step 3: Insert Test Data (Optional)

```sql
-- Add sample customers for testing
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "
INSERT INTO CustomerLoyalty (AccountNumber, TotalPoints, Tier, LastUpdated) VALUES 
('8765432134', 2500, 2, GETUTCDATE()),
('1234567890', 5500, 3, GETUTCDATE()),
('9876543210', 150, 1, GETUTCDATE()),
('5555555555', 7500, 4, GETUTCDATE()),
('1111111111', 0, 1, GETUTCDATE())
"
```

### Step 4: Verify Database Setup

#### Check if KeyLoyaltyDB Database Exists
```sql
-- Method 1: Check specific database
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name FROM sys.databases WHERE name = 'KeyLoyaltyDB'"

-- Method 2: List all databases on the SQL Server instance
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name FROM sys.databases ORDER BY name"

-- Method 3: List all databases in OmniChannelDB2 on the SQL Server instance
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d OmniChannelDB2 -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"

-- Method 3: Check tables in KeystoneOmniTransactionson the SQL Server instance
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d OmniChannelDB2 -Q "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KeystoneOmniTransactions' ORDER BY ORDINAL_POSITION"
COLUMN_NAME                                                                                                                      DATA_TYPE                                                                                            IS_NULLABLE


-- Method 3: Check if transaction type exist in the KeystoneOmniTransactions  db
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d OmniChannelDB2 -Q "SELECT DISTINCT Transactiontype, COUNT(*) as Count FROM KeystoneOmniTransactions WHERE Txnstatus = '00' GROUP BY Transactiontype ORDER BY Count DESC"
Transactiontype                                                                                      Count    


-- Method 3: Check database status
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name, database_id, create_date FROM sys.databases WHERE name = 'KeyLoyaltyDB'"
```

#### Expected Output for Database Existence:
```
name
----------------------------------------------------------------
KeyLoyaltyDB

(1 rows affected)
```

#### Check if CustomerLoyalty Table Exists
```sql
-- Check if table exists in KeyLoyaltyDB
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CustomerLoyalty'"

-- Get table structure
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CustomerLoyalty'"
```

#### View Database Contents
```sql
-- View all customer data
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "SELECT * FROM CustomerLoyalty ORDER BY AccountNumber"

-- Count total customers
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d KeyLoyaltyDB -Q "SELECT COUNT(*) as TotalCustomers FROM CustomerLoyalty"
```

## Application Configuration

### Step 5: Update Connection String

Update the `appsettings.json` file in the KeyLoyalty.API project:

```json
{
  "ConnectionStrings": {
    "OmniDbConnection": "Server=10.40.14.22,1433;Database=KeyLoyaltyDB;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;"
  }
}
```

## Database Schema Details

### CustomerLoyalty Table Structure

| Column Name    | Data Type      | Constraints                    | Description                           |
|----------------|----------------|--------------------------------|---------------------------------------|
| AccountNumber  | NVARCHAR(50)   | PRIMARY KEY                    | Customer's 10-digit account number   |
| TotalPoints    | INT            | NOT NULL, DEFAULT 0            | Customer's accumulated loyalty points |
| Tier           | INT            | NOT NULL, DEFAULT 1            | Customer's loyalty tier (1-5)         |
| LastUpdated    | DATETIME2      | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp                 |

### Loyalty Tier Mapping

| Tier | Name     | Point Range    | Icon |
|------|----------|----------------|------|
| 1    | Bronze   | 0 - 500        | 🥉   |
| 2    | Silver   | 501 - 3,000    | 🥈   |
| 3    | Gold     | 3,001 - 6,000  | 🥇   |
| 4    | Platinum | 6,001 - 10,000 | 💎   |
| 5    | Diamond  | 10,001+        | 💍   |

## Testing the Setup

### Step 6: Build and Run the Application

```bash
# Navigate to the API project
cd C:\Users\nosarinmwian\Downloads\loyatyEvent\loyatyEvent\src\KeyLoyalty.API

# Build the project
dotnet build

# Run the application
dotnet run
```

### Step 7: Test API Endpoints

Once the application is running, you can test these endpoints:

1. **Get Customer Dashboard** (existing customer):
   ```
   GET /api/loyalty/dashboard/8765432134
   ```
   Expected: Returns customer data with 2500 points, Silver tier

2. **Get Customer Dashboard** (non-existent customer):
   ```
   GET /api/loyalty/dashboard/9999999999
   ```
   Expected: Returns 404 error with message "Customer with account number 9999999999 not found in the loyalty program."

3. **Test NIP Transfer Processing** (for testing only - awards 2 points):
   ```
   POST /api/loyalty/process-transaction
   {
     "transactionId": "TXN123",
     "accountNumber": "2222222222",
     "transactionType": "NIP_TRANSFER",
     "amount": 1000,
     "username": "testuser"
   }
   ```

4. **Test Airtime Processing** (for testing only - awards 1 point):
   ```
   POST /api/loyalty/process-airtime
   {
     "transactionId": "AIR123",
     "accountNumber": "2222222222",
     "amount": 500,
     "phoneNumber": "08012345678",
     "network": "MTN",
     "username": "testuser"
   }
   ```

5. **Test Bill Payment Processing** (for testing only - awards 3 points):
   ```
   POST /api/loyalty/process-billpayment
   {
     "transactionId": "BILL123",
     "accountNumber": "2222222222",
     "amount": 2000,
     "billerName": "EKEDC",
     "billType": "ELECTRICITY",
     "username": "testuser"
   }
   ```

**Important**: These API endpoints are for testing the loyalty system only. In production, your existing banking system will send events to the loyalty system automatically.

## Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify SQL Server is running on `10.40.14.22:1433`
   - Check credentials: `DevSol` / `DevvSol1234`
   - Ensure firewall allows connection to port 1433

2. **Database Not Found Error**
   - Run: `sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name FROM sys.databases WHERE name = 'KeyLoyaltyDB'"`
   - If no results, create database using Step 1
   - Check if you have permissions to access the database

3. **Table Not Found Error**
   - Verify you're connected to the correct database (`KeyLoyaltyDB`)
   - Check if table exists using the verification queries above
   - If table doesn't exist, run Step 2 to create it

3. **Application Won't Start**
   - Check connection string in `appsettings.json`
   - Ensure all NuGet packages are restored: `dotnet restore`
   - Verify .NET 9.0 SDK is installed

### Database Verification Commands

```bash
# Check if SQL Server is accessible
telnet 10.40.14.22 1433

# Quick database existence check
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "IF DB_ID('KeyLoyaltyDB') IS NOT NULL PRINT 'KeyLoyaltyDB EXISTS' ELSE PRINT 'KeyLoyaltyDB DOES NOT EXIST'"

# List all databases 
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name FROM sys.databases ORDER BY name"

# Check database size and status
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT name, database_id, create_date, state_desc FROM sys.databases WHERE name = 'KeyLoyaltyDB'"

# Drop database (if needed to start over)
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "DROP DATABASE KeyLoyaltyDB"
```

### Using SQL Server Management Studio (SSMS)

1. **Connect to Server**: `10.40.14.22,1433`
2. **Login**: `DevSol` / `DevvSol1234`
3. **Look for**: `KeyLoyaltyDB` in the Databases folder
4. **Expand**: `KeyLoyaltyDB` → `Tables` → `dbo.CustomerLoyalty`
5. **Right-click table** → `Select Top 1000 Rows` to view data

## Event Processing Flow

The system processes loyalty events in this order:

1. **Transaction Event** → Points awarded based on transaction type
2. **Database Update** → Customer record created/updated
3. **Tier Calculation** → Tier updated based on total points
4. **Response** → Updated loyalty information returned

### Point Earning System Explained

#### How Points Are Calculated

Points are awarded **per transaction**, not based on amount. Each successful transaction earns a fixed number of points:

| Transaction Type | Points Earned | Conditions | How Loyalty System Gets Data |
|------------------|---------------|------------|------------------------------|
| Airtime Purchase | 1 point | Any amount | Listens to your airtime events |
| Data Purchase | 1 point | Any amount | Listens to your data events |
| NIP Transfer | 2 points | **Amount ≥ ₦1,000** | Listens to your transfer events |
| NIP Transfer | 0 points | Amount < ₦1,000 | Listens to your transfer events |
| Bill Payment | 3 points | Any amount | Listens to your bill payment events |

**Note**: The API endpoints (`/api/loyalty/process-*`) are for testing purposes only. In production, the loyalty system automatically listens to your banking system's transaction events.

#### Point Earning Examples

**Example 1: Customer makes 5 transfers**
- 3 NIP Transfers of ₦2,000 each × 2 points = 6 points
- 2 NIP Transfers of ₦500 each × 0 points = 0 points
- **Total: 6 points** (only transfers ≥ ₦1,000 earn points)

**Example 2: Customer does mixed transactions**
- 3 Airtime purchases (₦200 each) × 1 point = 3 points
- 2 NIP transfers (₦5,000 each) × 2 points = 4 points  
- 1 Bill payment (₦1,500) × 3 points = 3 points
- **Total: 10 points**

**Example 3: Understanding the ₦1,000 minimum**
- Transfer ₦999 = 0 points (❌ Below minimum)
- Transfer ₦1,000 = 2 points (✅ Meets minimum)
- Transfer ₦50,000 = 2 points (✅ Above minimum, still 2 points)

#### Integration with Your Existing Banking System

**The loyalty system is designed to LISTEN to your existing transactions, not replace them.**

**Option 1: Event-Driven Integration (Recommended)**

In your existing banking system, publish events after successful transactions:

```csharp
// In your existing Transfer Service
public async Task ProcessTransfer(TransferRequest request)
{
    // Your existing transfer logic
    var result = await _transferService.ProcessAsync(request);
    
    if (result.IsSuccessful)
    {
        // Publish event for loyalty system to listen
        await _eventPublisher.PublishAsync(new TransactionCompletedEvent
        {
            TransactionId = result.TransactionId,
            AccountNumber = request.DebitAccount,
            TransactionType = "NIP_TRANSFER",
            Amount = request.Amount,
            Username = request.Username,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

```csharp
// In your existing Airtime Service
public async Task ProcessAirtime(AirtimeRequest request)
{
    // Your existing airtime logic
    var result = await _airtimeService.ProcessAsync(request);
    
    if (result.IsSuccessful)
    {
        // Publish event for loyalty system
        await _eventPublisher.PublishAsync(new AirtimeCompletedEvent
        {
            TransactionId = result.TransactionId,
            AccountNumber = request.CustomerAccount,
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            Network = request.Network,
            Username = request.Username,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

**Option 2: Database Trigger Integration**

If your banking system logs transactions to a database, create triggers:

```sql
-- Trigger on your existing transaction table
CREATE TRIGGER tr_TransactionCompleted
ON YourBankingDB.dbo.Transactions
AFTER INSERT
AS
BEGIN
    INSERT INTO KeyLoyaltyDB.dbo.TransactionEvents (
        TransactionId, AccountNumber, TransactionType, 
        Amount, Username, CreatedDate
    )
    SELECT 
        TransactionId, DebitAccount, TransactionType,
        Amount, Username, GETUTCDATE()
    FROM inserted
    WHERE Status = 'SUCCESS'
END
```

**Option 3: Message Queue Integration**

Use RabbitMQ, Azure Service Bus, or similar:

```csharp
// In your existing banking services
public async Task ProcessTransaction(TransactionRequest request)
{
    // Your existing logic
    var result = await ProcessBankingTransaction(request);
    
    if (result.IsSuccessful)
    {
        // Send message to loyalty queue
        await _messageQueue.SendAsync("loyalty-queue", new
        {
            TransactionId = result.TransactionId,
            AccountNumber = request.AccountNumber,
            TransactionType = request.Type,
            Amount = request.Amount,
            Username = request.Username
        });
    }
}
```

#### Point Accumulation Process

1. **Customer performs transaction** (Transfer, Airtime, Bill Payment)
2. **Your system processes transaction** successfully
3. **Your system calls loyalty API** with transaction details
4. **Loyalty system awards points** based on transaction type
5. **Customer points are updated** in database
6. **Tier is recalculated** if points cross tier thresholds
7. **Customer can view updated points** via dashboard API

#### Tier Progression Example

**New Customer Journey:**
- Starts with: 0 points, Bronze Tier (🥉)
- Makes 10 transfers (₦5,000 each): 20 points, Bronze Tier (🥉)
- Makes 200 more transfers (₦2,000 each): 420 points, Bronze Tier (🥉)
- Makes 50 more transfers (₦1,500 each): 520 points, **Silver Tier (🥈)**
- Makes 1000 more transfers (₦1,000 each): 2520 points, Silver Tier (🥈)
- Makes 200 bill payments (any amount): 3120 points, **Gold Tier (🥇)**

## Maintenance

### Regular Tasks

1. **Backup Database**:
   ```sql
   BACKUP DATABASE KeyLoyaltyDB TO DISK = 'C:\Backup\KeyLoyaltyDB.bak'
   ```

2. **Monitor Table Growth**:
   ```sql
   SELECT COUNT(*) as TotalCustomers FROM CustomerLoyalty
   ```

3. **Check Recent Activity**:
   ```sql
   SELECT TOP 10 * FROM CustomerLoyalty ORDER BY LastUpdated DESC
   ```

## Security Considerations

- Change default credentials in production
- Use Windows Authentication where possible
- Implement proper backup and recovery procedures
- Monitor database access logs
- Consider encrypting sensitive data

---

**Note**: This setup is for development/testing purposes. For production deployment, additional security measures and performance optimizations should be implemented.