@echo off
echo ========================================
echo KeyLoyalty End-to-End Test Commands
echo ========================================
echo.

echo 1. Test Airtime Purchase (Low Amount - No Points)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"amount\":500,\"transactionType\":\"AIRTIME_PURCHASE\"}"
echo.
echo.

echo 2. Test Airtime Purchase (High Amount - 2000 Points)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"amount\":2000,\"transactionType\":\"AIRTIME_PURCHASE\"}"
echo.
echo.

echo 3. Test Bill Payment (1500 Points)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"amount\":1500,\"transactionType\":\"BILL_PAYMENT\"}"
echo.
echo.

echo 4. Test NIP Transfer Below Minimum (No Points)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"amount\":999,\"transactionType\":\"NIP_TRANSFER\"}"
echo.
echo.

echo 5. Test NIP Transfer Above Minimum (5000 Points)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"amount\":5000,\"transactionType\":\"NIP_TRANSFER\"}"
echo.
echo.

echo 6. Check Dashboard (Should show 8500 points, Gold tier)
curl -X GET http://localhost:5000/api/loyalty/dashboard/USER123 ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE"
echo.
echo.

echo 7. Test Redemption (Redeem 1000 points)
curl -X POST http://localhost:5000/api/loyalty/redeem-points ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE" ^
  -d "{\"accountNumber\":\"1234567890\",\"pointsToRedeem\":1000,\"redemptionType\":\"CASH\",\"username\":\"testuser\"}"
echo.
echo.

echo 8. Check Dashboard After Redemption (Should show 7500 points)
curl -X GET http://localhost:5000/api/loyalty/dashboard/USER123 ^
  -H "X-API-Key: KL-2024-API-KEY-SECURE"
echo.
echo.

echo ========================================
echo Test Complete
echo ========================================