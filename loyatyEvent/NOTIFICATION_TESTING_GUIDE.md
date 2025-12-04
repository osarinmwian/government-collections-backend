# Loyalty Redemption Notification Testing Guide

This guide helps you verify that email and OTP notifications work correctly after loyalty points are redeemed.

## Overview

The loyalty system should send notifications when:
1. **Points are redeemed** - Alert/email confirmation
2. **OTP verification is required** - SMS/Email OTP for security
3. **Account is credited** - Confirmation of successful redemption

## Quick Test

### Option 1: PowerShell Script (Recommended)
```powershell
# Run the automated test script
.\test-loyalty-notifications.ps1 -TestAccountNumber "1234567890" -TestEmail "your-email@example.com" -TestPhone "08012345678"
```

### Option 2: API Testing
```bash
# Test redemption notifications
curl -X POST "http://localhost:5000/api/notificationtest/test-redemption-notifications" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: KL-2024-API-KEY-SECURE-PRODUCTION" \
  -d '{
    "accountNumber": "1234567890",
    "username": "testuser",
    "email": "test@example.com",
    "phoneNumber": "08012345678",
    "pointsToRedeem": 50,
    "redemptionType": "TRANSFER"
  }'
```

### Option 3: Manual Testing
1. **Redeem Points via API**
   ```bash
   curl -X POST "http://localhost:5000/api/loyalty/redeem" \
     -H "Content-Type: application/json" \
     -d '{
       "accountNumber": "1234567890",
       "username": "testuser",
       "pointsToRedeem": 100,
       "redemptionType": "TRANSFER"
     }'
   ```

2. **Check for notifications** (see verification steps below)

## Verification Steps

### 1. Check Database Records
Execute these SQL queries to verify notification records:

```sql
-- Check OTP records
SELECT TOP 5 
    username, email, mobilenumber, action, datecreated, OTPStatus, OTPPurpose
FROM UserOTP 
WHERE action LIKE '%Loyalty%' OR OTPPurpose LIKE '%Loyalty%'
ORDER BY datecreated DESC;

-- Check email logs
SELECT TOP 5 
    username, otpAction, emailAddress, date_Sent
FROM OtpByEmailLogs 
WHERE otpAction LIKE '%Loyalty%'
ORDER BY date_Sent DESC;

-- Check loyalty alerts (if implemented)
SELECT TOP 5 
    UserId, AccountNumber, AlertType, Message, CreatedDate
FROM LoyaltyAlerts 
WHERE AlertType IN ('REDEMPTION', 'EARNING')
ORDER BY CreatedDate DESC;
```

### 2. Check Application Logs
Look for these log entries:
- `Point redemption alert sent to user`
- `OTP sent successfully`
- `Email notification sent`
- `SMTP mail sending attempt`

### 3. Verify Actual Delivery
- **Email**: Check inbox for redemption confirmation
- **SMS**: Verify OTP received on phone
- **Account**: Confirm amount was credited

## Configuration Requirements

### 1. Email Configuration
Ensure these settings are configured in `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@yourbank.com",
    "EnableSsl": true
  }
}
```

### 2. SMS/OTP Configuration
Verify SMS gateway settings in OmniChannel configuration:
- SMS provider credentials
- OTP timeout settings
- Phone number validation

### 3. Database Tables
Ensure these tables exist:
- `UserOTP` - For OTP records
- `OtpByEmailLogs` - For email notification logs
- `LoyaltyAlerts` - For loyalty-specific alerts (optional)
- `CustomerLoyalty` - For loyalty data

## Troubleshooting

### Common Issues

#### 1. No OTP Received
**Check:**
- SMS gateway configuration
- Phone number format
- OTP service logs
- Database OTP records

**Solution:**
```sql
-- Check if OTP was created
SELECT * FROM UserOTP WHERE mobilenumber = 'YOUR_PHONE' ORDER BY datecreated DESC;
```

#### 2. No Email Received
**Check:**
- SMTP configuration
- Email consent settings
- Email template files
- Spam/junk folder

**Solution:**
```sql
-- Check email consent
SELECT emailConsent FROM CustomerDetails WHERE AccountNo = 'YOUR_ACCOUNT';

-- Check email logs
SELECT * FROM OtpByEmailLogs WHERE emailAddress = 'YOUR_EMAIL' ORDER BY date_Sent DESC;
```

#### 3. Redemption Fails
**Check:**
- Sufficient points balance
- Account number validity
- Service registration in DI
- Database connectivity

**Solution:**
```bash
# Check loyalty dashboard
curl -X GET "http://localhost:5000/api/loyalty/dashboard/YOUR_ACCOUNT"
```

### Log Analysis
Check these log files:
- `logs/keyloyalty-YYYYMMDD.log` - Loyalty service logs
- `logs/transactions-YYYYMMDD.log` - Transaction logs
- Application Insights (if configured)

## Test Scenarios

### Scenario 1: Successful Redemption
1. User has sufficient points
2. Redemption request is valid
3. Points are deducted
4. Account is credited
5. Notifications are sent
6. Database records are created

### Scenario 2: Insufficient Points
1. User attempts redemption
2. System checks balance
3. Redemption is rejected
4. Error notification is sent
5. No points are deducted

### Scenario 3: OTP Verification
1. Redemption requires OTP
2. OTP is generated and sent
3. User enters OTP
4. OTP is validated
5. Redemption proceeds
6. Confirmation is sent

## Monitoring

### Key Metrics to Monitor
- OTP delivery success rate
- Email delivery success rate
- Redemption completion rate
- Notification failure rate

### Alerts to Set Up
- Failed OTP deliveries
- Failed email deliveries
- High redemption failure rate
- Database connection issues

## Production Checklist

Before deploying to production:

- [ ] SMTP settings configured and tested
- [ ] SMS gateway configured and tested
- [ ] Email templates deployed
- [ ] Database tables created
- [ ] Service dependencies registered
- [ ] Logging configured
- [ ] Monitoring alerts set up
- [ ] Error handling tested
- [ ] Load testing completed

## Support

If notifications are not working:

1. Run the configuration verification script:
   ```powershell
   .\verify-notification-config.ps1
   ```

2. Check the test results and follow recommendations

3. Review application logs for errors

4. Verify database connectivity and table structure

5. Test SMTP/SMS gateway connectivity separately

## API Endpoints for Testing

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/loyalty/dashboard/{account}` | GET | Check current points |
| `/api/loyalty/redeem` | POST | Redeem points |
| `/api/notificationtest/test-redemption-notifications` | POST | Full notification test |
| `/api/notificationtest/verification-queries` | GET | Get DB verification queries |
| `/api/notificationtest/test-alerts` | POST | Test alert service |

## Sample Test Data

```json
{
  "testAccount": "1234567890",
  "testUser": "testuser123",
  "testEmail": "test@example.com",
  "testPhone": "08012345678",
  "testPoints": 100,
  "redemptionTypes": ["TRANSFER", "AIRTIME", "BILL_PAYMENT"]
}
```

This guide should help you thoroughly test and verify that email and OTP notifications work correctly after loyalty redemption.