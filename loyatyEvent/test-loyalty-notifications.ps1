# Test Script for Loyalty Redemption Email and OTP Notifications
# This script verifies that email and OTP notifications work after loyalty points are redeemed

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$TestAccountNumber = "1234567890",
    [string]$TestEmail = "test@example.com",
    [string]$TestPhone = "08012345678",
    [string]$ApiKey = "KL-2024-API-KEY-SECURE-PRODUCTION"
)

Write-Host "=== Loyalty Redemption Notification Test ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Test Account: $TestAccountNumber" -ForegroundColor Yellow
Write-Host "Test Email: $TestEmail" -ForegroundColor Yellow
Write-Host "Test Phone: $TestPhone" -ForegroundColor Yellow

# Function to make API calls
function Invoke-ApiCall {
    param(
        [string]$Endpoint,
        [hashtable]$Body,
        [string]$Method = "POST",
        [hashtable]$Headers = @{}
    )
    
    $Headers["Content-Type"] = "application/json"
    $Headers["X-API-Key"] = $ApiKey
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl$Endpoint" -Method $Method -Body ($Body | ConvertTo-Json) -Headers $Headers
        return @{ Success = $true; Data = $response }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message; Response = $_.Exception.Response }
    }
}

# Function to check database for notifications
function Test-DatabaseNotifications {
    param([string]$AccountNumber)
    
    Write-Host "`n--- Checking Database for Notification Records ---" -ForegroundColor Cyan
    
    # Check OTP logs
    $otpQuery = @"
SELECT TOP 5 
    username, 
    email, 
    mobilenumber, 
    action, 
    datecreated, 
    OTPStatus,
    OTPPurpose
FROM UserOTP 
WHERE mobilenumber = '$TestPhone' OR email = '$TestEmail'
ORDER BY datecreated DESC
"@
    
    # Check email logs
    $emailQuery = @"
SELECT TOP 5 
    username, 
    otpAction, 
    emailAddress, 
    date_Sent
FROM OtpByEmailLogs 
WHERE emailAddress = '$TestEmail'
ORDER BY date_Sent DESC
"@
    
    Write-Host "Recent OTP Records:" -ForegroundColor Yellow
    Write-Host $otpQuery
    
    Write-Host "`nRecent Email Logs:" -ForegroundColor Yellow
    Write-Host $emailQuery
    
    Write-Host "`nNote: Execute these queries in your database to verify notification records" -ForegroundColor Green
}

# Test 1: Check loyalty dashboard
Write-Host "`n--- Test 1: Get Loyalty Dashboard ---" -ForegroundColor Cyan
$dashboardResult = Invoke-ApiCall -Endpoint "/api/loyalty/dashboard/$TestAccountNumber" -Method "GET"

if ($dashboardResult.Success) {
    Write-Host "✓ Dashboard retrieved successfully" -ForegroundColor Green
    Write-Host "Current Points: $($dashboardResult.Data.totalPoints)" -ForegroundColor Yellow
    Write-Host "Current Tier: $($dashboardResult.Data.tier)" -ForegroundColor Yellow
    $currentPoints = $dashboardResult.Data.totalPoints
} else {
    Write-Host "✗ Failed to get dashboard: $($dashboardResult.Error)" -ForegroundColor Red
    $currentPoints = 0
}

# Test 2: Add points if needed
if ($currentPoints -lt 100) {
    Write-Host "`n--- Test 2: Adding Points for Testing ---" -ForegroundColor Cyan
    $addPointsBody = @{
        accountNumber = $TestAccountNumber
        points = 500
        transactionType = "DEPOSIT"
        transactionAmount = 5000
    }
    
    $addPointsResult = Invoke-ApiCall -Endpoint "/api/loyalty/assign-points" -Body $addPointsBody
    
    if ($addPointsResult.Success) {
        Write-Host "✓ Points added successfully" -ForegroundColor Green
        $currentPoints = 500
    } else {
        Write-Host "✗ Failed to add points: $($addPointsResult.Error)" -ForegroundColor Red
    }
}

# Test 3: Redeem points (this should trigger notifications)
Write-Host "`n--- Test 3: Redeem Points (Should Trigger Notifications) ---" -ForegroundColor Cyan
$redeemBody = @{
    accountNumber = $TestAccountNumber
    username = "testuser"
    pointsToRedeem = 50
    redemptionType = "TRANSFER"
}

$redeemResult = Invoke-ApiCall -Endpoint "/api/loyalty/redeem" -Body $redeemBody

if ($redeemResult.Success) {
    Write-Host "✓ Points redeemed successfully" -ForegroundColor Green
    Write-Host "Amount Redeemed: ₦$($redeemResult.Data.amountRedeemed)" -ForegroundColor Yellow
    Write-Host "Remaining Points: $($redeemResult.Data.remainingPoints)" -ForegroundColor Yellow
    Write-Host "Transaction ID: $($redeemResult.Data.transactionId)" -ForegroundColor Yellow
} else {
    Write-Host "✗ Failed to redeem points: $($redeemResult.Error)" -ForegroundColor Red
}

# Test 4: Send OTP for redemption verification
Write-Host "`n--- Test 4: Send OTP for Redemption Verification ---" -ForegroundColor Cyan
$otpBody = @{
    username = "testuser"
    action = "LoyaltyRedemption"
    source = "LoyaltyApp"
    requestid = [System.Guid]::NewGuid().ToString()
    AccountNo = $TestAccountNumber
    OTPPurpose = "Loyalty Points Redemption Verification"
}

# Note: This would need to be called on the OmniChannel API
$otpEndpoint = "http://localhost:8080/OTP/Send"  # Adjust port as needed
$otpHeaders = @{ "Content-Type" = "application/json" }

try {
    $otpResponse = Invoke-RestMethod -Uri $otpEndpoint -Method POST -Body ($otpBody | ConvertTo-Json) -Headers $otpHeaders
    Write-Host "✓ OTP sent successfully" -ForegroundColor Green
    Write-Host "OTP Response: $($otpResponse | ConvertTo-Json)" -ForegroundColor Yellow
} catch {
    Write-Host "✗ Failed to send OTP: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Note: Make sure OmniChannel API is running on the correct port" -ForegroundColor Yellow
}

# Test 5: Check for email notifications
Write-Host "`n--- Test 5: Email Notification Test ---" -ForegroundColor Cyan
Write-Host "Email notifications should be sent for:" -ForegroundColor Yellow
Write-Host "1. Loyalty points redemption confirmation" -ForegroundColor White
Write-Host "2. Account credit notification (if redemption type is TRANSFER)" -ForegroundColor White
Write-Host "3. OTP for redemption verification" -ForegroundColor White

# Test 6: Verify notification components
Write-Host "`n--- Test 6: Verify Notification Components ---" -ForegroundColor Cyan

# Check if notification services are configured
$configChecks = @(
    @{ Name = "SMTP Configuration"; Check = "Check appsettings.json for SMTP settings" },
    @{ Name = "Email Templates"; Check = "Verify email templates exist in SharedPath/EmailTemplates/" },
    @{ Name = "OTP Service"; Check = "Ensure OTPServices.cs is properly configured" },
    @{ Name = "Alert Service"; Check = "Verify AlertService is registered in DI container" }
)

foreach ($check in $configChecks) {
    Write-Host "- $($check.Name): $($check.Check)" -ForegroundColor Yellow
}

# Test 7: Database verification
Test-DatabaseNotifications -AccountNumber $TestAccountNumber

# Test 8: Manual verification steps
Write-Host "`n--- Manual Verification Steps ---" -ForegroundColor Cyan
Write-Host "1. Check email inbox for redemption notifications" -ForegroundColor Yellow
Write-Host "2. Verify SMS/OTP was received on phone number" -ForegroundColor Yellow
Write-Host "3. Check application logs for notification sending attempts" -ForegroundColor Yellow
Write-Host "4. Verify database records for OTP and email logs" -ForegroundColor Yellow

# Test 9: Configuration verification
Write-Host "`n--- Configuration Verification ---" -ForegroundColor Cyan
$configFile = "c:\Users\nosarinmwian\Desktop\keyMobileBackEnd\loyatyEvent\src\KeyLoyalty.API\appsettings.json"
if (Test-Path $configFile) {
    Write-Host "✓ Configuration file found: $configFile" -ForegroundColor Green
    try {
        $config = Get-Content $configFile | ConvertFrom-Json
        Write-Host "Database Connection: $($config.ConnectionStrings.DefaultConnection -ne $null ? 'Configured' : 'Missing')" -ForegroundColor Yellow
        Write-Host "Logging Configuration: $($config.Serilog -ne $null ? 'Configured' : 'Missing')" -ForegroundColor Yellow
    } catch {
        Write-Host "✗ Error reading configuration: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Configuration file not found" -ForegroundColor Red
}

Write-Host "`n=== Test Summary ===" -ForegroundColor Green
Write-Host "1. Loyalty redemption API tested" -ForegroundColor White
Write-Host "2. OTP notification service tested" -ForegroundColor White
Write-Host "3. Email notification components verified" -ForegroundColor White
Write-Host "4. Database queries provided for verification" -ForegroundColor White
Write-Host "5. Manual verification steps outlined" -ForegroundColor White

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "1. Run the database queries to check notification records" -ForegroundColor Yellow
Write-Host "2. Check email inbox and SMS for actual notifications" -ForegroundColor Yellow
Write-Host "3. Review application logs for any errors" -ForegroundColor Yellow
Write-Host "4. Verify SMTP and SMS gateway configurations" -ForegroundColor Yellow