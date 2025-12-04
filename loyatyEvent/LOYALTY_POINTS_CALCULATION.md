# Loyalty Points Calculation System

## Overview
The loyalty system awards points based on transaction type and amount, with bonus multipliers for higher transaction values.

## Point Earning Rules

### Base Points by Transaction Type
- **Airtime/Data**: 1 point
- **Bill Payment**: 3 points
- **Transfer/NIP**: 2 points
- **Deposit**: 1 point

### Amount-Based Bonus Multipliers
| Transaction Amount | Multiplier |
|-------------------|------------|
| ₦50,000+          | 3x         |
| ₦10,000+          | 2x         |
| ₦5,000+           | 1.5x       |
| ₦1,000+           | 1x (base)  |
| Under ₦1,000      | 0.5x       |

### Minimum Transaction
- **₦100** minimum to earn any points
- Transactions below ₦100 earn **0 points**

## Calculation Formula
```
Final Points = Base Points × Bonus Multiplier
Result = Math.Ceiling(calculated value)
```

## Examples

### Airtime Transactions
- ₦500 Airtime = 1 × 0.5 = **1 point**
- ₦2,000 Airtime = 1 × 1 = **1 point**
- ₦8,000 Airtime = 1 × 1.5 = **2 points**
- ₦15,000 Airtime = 1 × 2 = **2 points**
- ₦60,000 Airtime = 1 × 3 = **3 points**

### Bill Payment Transactions
- ₦800 Bill = 3 × 0.5 = **2 points**
- ₦3,000 Bill = 3 × 1 = **3 points**
- ₦7,000 Bill = 3 × 1.5 = **5 points**
- ₦12,000 Bill = 3 × 2 = **6 points**
- ₦55,000 Bill = 3 × 3 = **9 points**

### Transfer Transactions
- ₦600 Transfer = 2 × 0.5 = **1 point**
- ₦4,000 Transfer = 2 × 1 = **2 points**
- ₦6,000 Transfer = 2 × 1.5 = **3 points**
- ₦20,000 Transfer = 2 × 2 = **4 points**
- ₦75,000 Transfer = 2 × 3 = **6 points**

## Point Redemption

### Redemption Value
- **All redemption types**: ₦1.00 per point

### Redemption Options
- **Airtime**: ₦1.00 per point
- **Bill Payment**: ₦1.00 per point
- **Transfer**: ₦1.00 per point (credited to account)

## Loyalty Tiers

| Tier     | Points Range    | Icon |
|----------|----------------|------|
| Bronze   | 0 - 500        | 🥉   |
| Silver   | 501 - 3,000    | 🥈   |
| Gold     | 3,001 - 6,000  | 🥇   |
| Platinum | 6,001 - 10,000 | 💎   |
| Diamond  | 10,001+        | 💍   |

## Technical Implementation

### Code Location
- **Point Calculation**: `LoyaltyApplicationService.CalculatePointsForTransaction()`
- **Point Assignment**: `LoyaltyApplicationService.AssignPointsAsync()`
- **Point Redemption**: `LoyaltyApplicationService.RedeemPointsAsync()`

### Database Tables
- **CustomerLoyalty**: Stores user points and tier
- **ProcessedTransactions**: Prevents duplicate point awards

### Transaction Processing
1. System monitors `KeystoneOmniTransactions` table
2. Processes new transactions every 30 seconds
3. Calculates points based on type and amount
4. Updates customer loyalty record
5. Prevents duplicate processing via transaction ID tracking