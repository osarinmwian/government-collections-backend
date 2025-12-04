using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

class SimpleTransactionReader
{
    private static readonly string BaseConnectionString = "Server=10.40.14.22,1433;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Reading Successful Transactions from Existing Databases ===");
        Console.WriteLine("This shows transactions that would generate loyalty points");
        Console.WriteLine();
        
        await ReadFromOmniChannelDB();
        await ReadFromAirtimeDB();
        await ReadFromBillPaymentDB();
        await ReadFromTransferDB();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static async Task ReadFromOmniChannelDB()
    {
        var connectionString = $"{BaseConnectionString};Database=OmniChannelDB2;";
        Console.WriteLine("=== OMNICHANNEL DATABASE (OmniChannelDB2) ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT TOP 5 * FROM Transactions WHERE Status = 'SUCCESS' ORDER BY CreatedDate DESC";
            var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            if (reader.HasRows)
            {
                Console.WriteLine("Recent Successful Transactions:");
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
                        var displayValue = value?.Length > 14 ? value.Substring(0, 14) : value;
                        Console.Write($"{displayValue,-15} ");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No successful transactions found.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"OmniChannelDB2 error: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task ReadFromAirtimeDB()
    {
        var connectionString = $"{BaseConnectionString};Database=AirtimeDB;";
        Console.WriteLine("=== AIRTIME DATABASE (AirtimeDB) ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 5 AccountNumber, Amount, TransactionId, TransactionDate, Status FROM AirtimeTransactions WHERE Status = 'SUCCESS' ORDER BY TransactionDate DESC",
                "SELECT TOP 5 * FROM Transactions WHERE Status = 'SUCCESS' ORDER BY CreatedDate DESC"
            };
            
            bool foundData = false;
            foreach (var sql in queries)
            {
                try
                {
                    var command = new SqlCommand(sql, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine("Successful Airtime Transactions:");
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
                                var displayValue = value?.Length > 14 ? value.Substring(0, 14) : value;
                                Console.Write($"{displayValue,-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException) { continue; }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No successful airtime transactions found or database not accessible.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"AirtimeDB error: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task ReadFromBillPaymentDB()
    {
        var connectionString = $"{BaseConnectionString};Database=BillPaymentDB;";
        Console.WriteLine("=== BILL PAYMENT DATABASE (BillPaymentDB) ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 5 AccountNumber, Amount, TransactionId, PaymentDate, BillType, Status FROM BillPaymentTransactions WHERE Status = 'SUCCESS' ORDER BY PaymentDate DESC",
                "SELECT TOP 5 * FROM Transactions WHERE Status = 'SUCCESS' ORDER BY CreatedDate DESC"
            };
            
            bool foundData = false;
            foreach (var sql in queries)
            {
                try
                {
                    var command = new SqlCommand(sql, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine("Successful Bill Payment Transactions:");
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
                                var displayValue = value?.Length > 14 ? value.Substring(0, 14) : value;
                                Console.Write($"{displayValue,-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException) { continue; }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No successful bill payment transactions found or database not accessible.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"BillPaymentDB error: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task ReadFromTransferDB()
    {
        var connectionString = $"{BaseConnectionString};Database=TransferDB;";
        Console.WriteLine("=== TRANSFER DATABASE (TransferDB) ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 5 FromAccount, Amount, TransactionId, TransferDate, TransferType, Status FROM TransferTransactions WHERE Status = 'SUCCESS' ORDER BY TransferDate DESC",
                "SELECT TOP 5 * FROM Transactions WHERE Status = 'SUCCESS' ORDER BY CreatedDate DESC"
            };
            
            bool foundData = false;
            foreach (var sql in queries)
            {
                try
                {
                    var command = new SqlCommand(sql, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (reader.HasRows)
                    {
                        foundData = true;
                        Console.WriteLine("Successful Transfer Transactions:");
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
                                var displayValue = value?.Length > 14 ? value.Substring(0, 14) : value;
                                Console.Write($"{displayValue,-15} ");
                            }
                            Console.WriteLine();
                        }
                        break;
                    }
                }
                catch (SqlException) { continue; }
            }
            
            if (!foundData)
            {
                Console.WriteLine("No successful transfer transactions found or database not accessible.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"TransferDB error: {ex.Message}");
        }
        Console.WriteLine();
    }
}