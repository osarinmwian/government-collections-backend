using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

class DatabaseLogger
{
    private static readonly string BaseConnectionString = "Server=10.40.14.22,1433;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Transaction Databases Logger ===");
        Console.WriteLine($"Server: 10.40.14.22");
        Console.WriteLine();
        
        try
        {
            await LogLoyaltyDatabase();
            await LogAirtimeTransactions();
            await LogBillPaymentTransactions();
            await LogTransferTransactions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static async Task LogLoyaltyDatabase()
    {
        var connectionString = $"{BaseConnectionString};Database=KeyLoyaltyDB;";
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        Console.WriteLine("=== LOYALTY DATABASE (KeyLoyaltyDB) ===");
        Console.WriteLine("CustomerLoyalty Table:");
        
        var sql = "SELECT AccountNumber, TotalPoints, Tier, LastUpdated FROM CustomerLoyalty ORDER BY LastUpdated DESC";
        var command = new SqlCommand(sql, connection);
        
        using var reader = await command.ExecuteReaderAsync();
        
        if (!reader.HasRows)
        {
            Console.WriteLine("No records found in CustomerLoyalty table.");
        }
        else
        {
            Console.WriteLine($"{"Account",-12} {"Points",-8} {"Tier",-4} {"Last Updated",-20}");
            Console.WriteLine(new string('-', 50));
            
            while (await reader.ReadAsync())
            {
                var accountNumber = reader.GetString(0);
                var totalPoints = reader.GetInt32(1);
                var tier = reader.GetInt32(2);
                var lastUpdated = reader.GetDateTime(3);
                
                Console.WriteLine($"{accountNumber,-12} {totalPoints,-8} {tier,-4} {lastUpdated:yyyy-MM-dd HH:mm}");
            }
        }
        Console.WriteLine();
    }
    
    static async Task LogAirtimeTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=AirtimeDB;";
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("=== AIRTIME DATABASE (AirtimeDB) ===");
            
            var tableQueries = new[]
            {
                "SELECT TOP 10 * FROM AirtimeTransactions WHERE Status = 'SUCCESS' ORDER BY TransactionDate DESC",
                "SELECT TOP 10 * FROM Transactions WHERE TransactionType = 'AIRTIME' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC",
                "SELECT TOP 10 * FROM AirtimePurchase WHERE Status = 'COMPLETED' ORDER BY PurchaseDate DESC"
            };
            
            bool foundData = false;
            foreach (var query in tableQueries)
            {
                try
                {
                    var command = new SqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine($"Query: {query.Split(' ')[3]}");
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i),-15} ");
                        }
                        Console.WriteLine();
                        Console.WriteLine(new string('-', reader.FieldCount * 16));
                        
                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                                Console.Write($"{value?.Substring(0, Math.Min(value.Length, 14)),-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException)
                {
                    continue;
                }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No airtime transaction tables found or no successful transactions.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"AirtimeDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task LogBillPaymentTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=BillPaymentDB;";
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("=== BILL PAYMENT DATABASE (BillPaymentDB) ===");
            
            var tableQueries = new[]
            {
                "SELECT TOP 10 * FROM BillPayments WHERE Status = 'SUCCESS' ORDER BY PaymentDate DESC",
                "SELECT TOP 10 * FROM Transactions WHERE TransactionType = 'BILLPAYMENT' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC",
                "SELECT TOP 10 * FROM PaymentTransactions WHERE Status = 'COMPLETED' ORDER BY TransactionDate DESC"
            };
            
            bool foundData = false;
            foreach (var query in tableQueries)
            {
                try
                {
                    var command = new SqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine($"Query: {query.Split(' ')[3]}");
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i),-15} ");
                        }
                        Console.WriteLine();
                        Console.WriteLine(new string('-', reader.FieldCount * 16));
                        
                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                                Console.Write($"{value?.Substring(0, Math.Min(value.Length, 14)),-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException)
                {
                    continue;
                }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No bill payment transaction tables found or no successful transactions.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"BillPaymentDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task LogTransferTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=TransferDB;";
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("=== TRANSFER DATABASE (TransferDB) ===");
            
            var tableQueries = new[]
            {
                "SELECT TOP 10 * FROM Transfers WHERE Status = 'SUCCESS' ORDER BY TransferDate DESC",
                "SELECT TOP 10 * FROM Transactions WHERE TransactionType = 'TRANSFER' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC",
                "SELECT TOP 10 * FROM MoneyTransfers WHERE Status = 'COMPLETED' ORDER BY TransactionDate DESC"
            };
            
            bool foundData = false;
            foreach (var query in tableQueries)
            {
                try
                {
                    var command = new SqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine($"Query: {query.Split(' ')[3]}");
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i),-15} ");
                        }
                        Console.WriteLine();
                        Console.WriteLine(new string('-', reader.FieldCount * 16));
                        
                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                                Console.Write($"{value?.Substring(0, Math.Min(value.Length, 14)),-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException)
                {
                    continue;
                }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No transfer transaction tables found or no successful transactions.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"TransferDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
}