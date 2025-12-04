# Notification Configuration Verification Script
# This script checks if email and OTP notification components are properly configured

Write-Host "=== Notification Configuration Verification ===" -ForegroundColor Green

$projectRoot = "c:\Users\nosarinmwian\Desktop\keyMobileBackEnd"

# Check 1: OTP Service Configuration
Write-Host "`n--- Checking OTP Service Configuration ---" -ForegroundColor Cyan
$otpServicePath = "$projectRoot\OmniChannel\OmniChannel\OmniChannel.Gateway\OTPServices.cs"
if (Test-Path $otpServicePath) {
    Write-Host "✓ OTP Service found: $otpServicePath" -ForegroundColor Green
    
    $otpContent = Get-Content $otpServicePath -Raw
    $checks = @(
        @{ Name = "Email Template Support"; Pattern = "EmailTemplateName"; Found = $otpContent -match "EmailTemplateName" },
        @{ Name = "SMTP Mail Sending"; Pattern = "SmtpSendMail"; Found = $otpContent -match "SmtpSendMail" },
        @{ Name = "Email Consent Check"; Pattern = "emailConsent"; Found = $otpContent -match "emailConsent" },
        @{ Name = "OTP Email Logging"; Pattern = "OTP_Via_EmailLogs"; Found = $otpContent -match "OTP_Via_EmailLogs" }
    )
    
    foreach ($check in $checks) {
        $status = if ($check.Found) { "✓" } else { "✗" }
        $color = if ($check.Found) { "Green" } else { "Red" }
        Write-Host "  $status $($check.Name)" -ForegroundColor $color
    }
} else {
    Write-Host "✗ OTP Service not found" -ForegroundColor Red
}

# Check 2: Alert Service Configuration
Write-Host "`n--- Checking Alert Service Configuration ---" -ForegroundColor Cyan
$alertServicePath = "$projectRoot\loyatyEvent\src\KeyLoyalty.Infrastructure\Services\AlertService.cs"
if (Test-Path $alertServicePath) {
    Write-Host "✓ Alert Service found: $alertServicePath" -ForegroundColor Green
    
    $alertContent = Get-Content $alertServicePath -Raw
    $alertChecks = @(
        @{ Name = "Point Earning Alerts"; Pattern = "SendPointEarningAlertAsync"; Found = $alertContent -match "SendPointEarningAlertAsync" },
        @{ Name = "Point Redemption Alerts"; Pattern = "SendPointRedemptionAlertAsync"; Found = $alertContent -match "SendPointRedemptionAlertAsync" },
        @{ Name = "Tier Upgrade Alerts"; Pattern = "SendTierUpgradeAlertAsync"; Found = $alertContent -match "SendTierUpgradeAlertAsync" },
        @{ Name = "Point Expiry Alerts"; Pattern = "SendPointExpiryAlertAsync"; Found = $alertContent -match "SendPointExpiryAlertAsync" }
    )
    
    foreach ($check in $alertChecks) {
        $status = if ($check.Found) { "✓" } else { "✗" }
        $color = if ($check.Found) { "Green" } else { "Red" }
        Write-Host "  $status $($check.Name)" -ForegroundColor $color
    }
} else {
    Write-Host "✗ Alert Service not found" -ForegroundColor Red
}

# Check 3: Configuration Files
Write-Host "`n--- Checking Configuration Files ---" -ForegroundColor Cyan

$configFiles = @(
    "$projectRoot\loyatyEvent\src\KeyLoyalty.API\appsettings.json",
    "$projectRoot\loyatyEvent\.env.template",
    "$projectRoot\OmniChannel\OmniChannel\OmniChannel.API\Web.config"
)

foreach ($configFile in $configFiles) {
    if (Test-Path $configFile) {
        Write-Host "✓ Config file found: $(Split-Path $configFile -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "✗ Config file missing: $(Split-Path $configFile -Leaf)" -ForegroundColor Red
    }
}

# Check 4: Email Template Directory
Write-Host "`n--- Checking Email Templates ---" -ForegroundColor Cyan
$templatePaths = @(
    "$projectRoot\OmniChannel\EmailTemplates",
    "$projectRoot\loyatyEvent\EmailTemplates",
    "C:\SharedPath\EmailTemplates"
)

$templateFound = $false
foreach ($templatePath in $templatePaths) {
    if (Test-Path $templatePath) {
        Write-Host "✓ Email templates directory found: $templatePath" -ForegroundColor Green
        $templates = Get-ChildItem $templatePath -Filter "*.html" -ErrorAction SilentlyContinue
        Write-Host "  Templates found: $($templates.Count)" -ForegroundColor Yellow
        $templateFound = $true
        break
    }
}

if (-not $templateFound) {
    Write-Host "✗ Email templates directory not found in common locations" -ForegroundColor Red
    Write-Host "  Check SharedPath configuration in app settings" -ForegroundColor Yellow
}

# Check 5: Database Tables
Write-Host "`n--- Database Table Verification Queries ---" -ForegroundColor Cyan
Write-Host "Execute these queries to verify notification tables exist:" -ForegroundColor Yellow

$dbQueries = @"
-- Check if OTP table exists
SELECT COUNT(*) as OTP_Table_Exists FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserOTP'

-- Check if Email logs table exists  
SELECT COUNT(*) as EmailLogs_Table_Exists FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OtpByEmailLogs'

-- Check if Loyalty alerts table exists
SELECT COUNT(*) as LoyaltyAlerts_Table_Exists FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LoyaltyAlerts'

-- Sample OTP records
SELECT TOP 5 username, email, mobilenumber, action, datecreated, OTPStatus 
FROM UserOTP 
ORDER BY datecreated DESC

-- Sample Email log records
SELECT TOP 5 username, otpAction, emailAddress, date_Sent 
FROM OtpByEmailLogs 
ORDER BY date_Sent DESC
"@

Write-Host $dbQueries -ForegroundColor White

# Check 6: Service Registration
Write-Host "`n--- Service Registration Check ---" -ForegroundColor Cyan
$programPath = "$projectRoot\loyatyEvent\src\KeyLoyalty.API\Program.cs"
if (Test-Path $programPath) {
    Write-Host "✓ Program.cs found" -ForegroundColor Green
    
    $programContent = Get-Content $programPath -Raw
    $serviceChecks = @(
        @{ Name = "Alert Service Registration"; Pattern = "AddScoped.*IAlertService"; Found = $programContent -match "AddScoped.*IAlertService" },
        @{ Name = "Loyalty Service Registration"; Pattern = "AddScoped.*ILoyaltyApplicationService"; Found = $programContent -match "AddScoped.*ILoyaltyApplicationService" },
        @{ Name = "Event Publisher Registration"; Pattern = "AddScoped.*IEventPublisher"; Found = $programContent -match "AddScoped.*IEventPublisher" }
    )
    
    foreach ($check in $serviceChecks) {
        $status = if ($check.Found) { "✓" } else { "?" }
        $color = if ($check.Found) { "Green" } else { "Yellow" }
        Write-Host "  $status $($check.Name)" -ForegroundColor $color
    }
} else {
    Write-Host "✗ Program.cs not found" -ForegroundColor Red
}

# Summary and Recommendations
Write-Host "`n=== Summary and Recommendations ===" -ForegroundColor Green

Write-Host "`nTo ensure email and OTP notifications work after loyalty redemption:" -ForegroundColor Cyan

Write-Host "`n1. Configuration Requirements:" -ForegroundColor Yellow
Write-Host "   - SMTP settings in appsettings.json" -ForegroundColor White
Write-Host "   - Email templates in SharedPath directory" -ForegroundColor White
Write-Host "   - SMS gateway configuration for OTP" -ForegroundColor White
Write-Host "   - Database connection strings" -ForegroundColor White

Write-Host "`n2. Service Integration:" -ForegroundColor Yellow
Write-Host "   - Register AlertService in DI container" -ForegroundColor White
Write-Host "   - Ensure OTPServices is accessible from loyalty service" -ForegroundColor White
Write-Host "   - Configure event publishing for redemption events" -ForegroundColor White

Write-Host "`n3. Testing Steps:" -ForegroundColor Yellow
Write-Host "   - Run the test-loyalty-notifications.ps1 script" -ForegroundColor White
Write-Host "   - Execute database verification queries" -ForegroundColor White
Write-Host "   - Check application logs for errors" -ForegroundColor White
Write-Host "   - Verify actual email/SMS delivery" -ForegroundColor White

Write-Host "`n4. Monitoring:" -ForegroundColor Yellow
Write-Host "   - Monitor OTP and email log tables" -ForegroundColor White
Write-Host "   - Set up alerts for notification failures" -ForegroundColor White
Write-Host "   - Track redemption success rates" -ForegroundColor White

Write-Host "`nConfiguration verification complete!" -ForegroundColor Green