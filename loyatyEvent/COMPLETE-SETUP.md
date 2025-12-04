# KeyLoyalty System - Complete Setup Guide

## Quick Start (3 Steps)

### 1. Setup Database & Start System
```bash
start-system.cmd
```

### 2. Test the System  
```bash
test-complete.cmd
```

### 3. Validate Results
```bash
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -i validate-system.sql
```

## System Overview

### Points Calculation Rules
- **AIRTIME_PURCHASE**: 1 point per ₦1 (minimum ₦1000)
- **BILL_PAYMENT**: 1 point per ₦1 (minimum ₦1000)  
- **NIP_TRANSFER**: 1 point per ₦1 (minimum ₦1000)

### Loyalty Tiers
- **Bronze**: 0-500 points 🥉
- **Silver**: 501-3,000 points 🥈
- **Gold**: 3,001-6,000 points 🥇
- **Platinum**: 6,001-10,000 points 💎
- **Diamond**: 10,001+ points 💍

## API Endpoints

### 1. Process Transaction
```bash
POST /api/transactions/process
{
  "accountNumber": "12338849440",
  "amount": 2000,
  "transactionType": "AIRTIME_PURCHASE"
}
```

### 2. Get Dashboard
```bash
GET /api/loyalty/dashboard/USER123
```

### 3. Redeem Points
```bash
POST /api/loyalty/redeem-points
{
  "accountNumber": "12338849440",
  "pointsToRedeem": 1000,
  "redemptionType": "CASH",
  "username": "testuser"
}
```

## Expected Test Results

### After Running test-complete.cmd:
1. **AIRTIME_PURCHASE ₦2000** → 2000 points
2. **BILL_PAYMENT ₦1500** → 1500 points  
3. **NIP_TRANSFER ₦5000** → 5000 points
4. **Total Points**: 8500 (Gold Tier)
5. **After Redemption**: 7500 points

### Dashboard Response:
```json
{
  "userId": "USER123",
  "accountNumbers": ["12338849440", "1234567890"],
  "totalPoints": 8500,
  "tier": "Gold",
  "tierIcon": "🥇",
  "pointsToNextTier": 1501
}
```

## Database Tables

### CustomerLoyalty
- Stores user points and tier
- Primary key: UserId

### AccountingEntries  
- GL accounting entries
- Uses GL: NGN1760001110001

### KeystoneOmniTransactions
- Account to UserId mapping
- Test data included

## Troubleshooting

### Database Connection Issues
```bash
# Test connection
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT 1"
```

### API Not Starting
```bash
# Check port availability
netstat -an | findstr :5000
```

### Invalid Transaction Type Error
- Use: AIRTIME_PURCHASE, BILL_PAYMENT, NIP_TRANSFER
- Not: airtime, bill, transfer

## Files Created
- `setup-database.sql` - Database setup
- `start-system.cmd` - System startup
- `test-complete.cmd` - End-to-end tests
- `validate-system.sql` - System validation

## Success Indicators
✅ Database tables created  
✅ API starts on port 5000  
✅ Swagger UI accessible  
✅ Points calculated correctly  
✅ GL accounting entries created  
✅ Dashboard shows user + accounts  
✅ Redemption works  
✅ Invalid types rejected