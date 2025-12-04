-- Reset ALL users' points by recalculating from actual transactions
-- This fixes duplicate point awards for all users

-- Step 1: Backup current data
SELECT * INTO CustomerLoyalty_Backup FROM CustomerLoyalty;

-- Step 2: Reset all points to 0
UPDATE CustomerLoyalty 
SET TotalPoints = 0,
    Tier = 0,  -- Bronze
    LastUpdated = GETDATE();

-- Step 3: Clear processed transactions to allow recalculation
TRUNCATE TABLE ProcessedTransactions;

-- Step 4: Verify reset
SELECT COUNT(*) as TotalUsers, 
       SUM(TotalPoints) as TotalPointsInSystem,
       AVG(TotalPoints) as AvgPoints
FROM CustomerLoyalty;

-- Step 5: Show users that had inflated points (backup vs current)
SELECT b.UserId, 
       b.TotalPoints as OldPoints, 
       c.TotalPoints as NewPoints,
       (b.TotalPoints - c.TotalPoints) as PointsRemoved
FROM CustomerLoyalty_Backup b
JOIN CustomerLoyalty c ON b.UserId = c.UserId
WHERE b.TotalPoints > c.TotalPoints
ORDER BY PointsRemoved DESC;