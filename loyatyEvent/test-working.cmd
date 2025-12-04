@echo off
echo ========================================
echo KeyLoyalty Complete End-to-End Test
echo Using Account: 1006817382 (oluwajoba)
echo ========================================
echo.

echo Step 1: Test AIRTIME_PURCHASE (1 point expected)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d" ^
  -d "{\"accountNumber\":\"1006817382\",\"amount\":2000,\"transactionType\":\"AIRTIME_PURCHASE\"}"
echo.
echo.

echo Step 2: Test BILL_PAYMENT (3 points expected)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d" ^
  -d "{\"accountNumber\":\"1006817382\",\"amount\":1500,\"transactionType\":\"BILL_PAYMENT\"}"
echo.
echo.

echo Step 3: Test NIP_TRANSFER (2 points expected)
curl -X POST http://localhost:5000/api/transactions/process ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d" ^
  -d "{\"accountNumber\":\"1006817382\",\"amount\":5000,\"transactionType\":\"NIP_TRANSFER\"}"
echo.
echo.

echo Step 4: Check Dashboard (Should show 6 points, Bronze tier)
curl -X GET http://localhost:5000/api/loyalty/dashboard/oluwajoba ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d"
echo.
echo.

echo Step 5: Test Redemption (Redeem 3 points)
curl -X POST http://localhost:5000/api/loyalty/redeem-points ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d" ^
  -d "{\"accountNumber\":\"1006817382\",\"pointsToRedeem\":3,\"redemptionType\":\"CASH\",\"username\":\"testuser\"}"
echo.
echo.

echo Step 6: Final Dashboard Check (Should show 3 points)
curl -X GET http://localhost:5000/api/loyalty/dashboard/oluwajoba ^
  -H "X-API-Key: KBL-7f9d4a2e8b1c6f3a9e5d2b8c4f7a1e6d9b3c8f2a5e7d1b4c9f6a3e8d2b5c7f0a4e9d"
echo.
echo.

echo ========================================
echo Test Complete - Check results above
echo Expected: 6 points after transactions, 3 points after redemption
echo ========================================