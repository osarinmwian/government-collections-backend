using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

class TransactionDatabaseLogger
{
    private static readonly string BaseConnectionString = "Server=10.40.14.22,1433;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Transaction Databases Reader ===");
        Console.WriteLine("Reading successful transactions from:");
        Console.WriteLine("- AirtimeDB");
        Console.WriteLine("- BillPaymentDB");
        Console.WriteLine("- TransferDB");
        Console.WriteLine();
        
        try
        {
            await ReadAirtimeTransactions();
            await ReadBillPaymentTransactions();
            await ReadTransferTransactions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static async Task ReadAirtimeTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=AirtimeDB;";
        Console.WriteLine("=== AIRTIME TRANSACTIONS ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 10 AccountNumber, Amount, TransactionId, TransactionDate, Status FROM AirtimeTransactions WHERE Status = 'SUCCESS' ORDER BY TransactionDate DESC",
                "SELECT TOP 10 AccountNumber, Amount, TransactionId, CreatedDate, Status FROM Transactions WHERE TransactionType = 'AIRTIME' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC"
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
                        Console.WriteLine($"{"Account",-12} {"Amount",-10} {"Transaction ID",-15} {"Date",-20} {"Status",-10}");
                        Console.WriteLine(new string('-', 70));
                        
                        while (await reader.ReadAsync())
                        {
                            var account = reader.GetString(0);
                            var amount = reader.GetDecimal(1);
                            var transactionId = reader.GetString(2);
                            var date = reader.GetDateTime(3);
                            var status = reader.GetString(4);
                            
                            Console.WriteLine($"{account,-12} {amount,-10:C} {transactionId,-15} {date:yyyy-MM-dd HH:mm,-20} {status,-10}");
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
                Console.WriteLine("No successful airtime transactions found.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"AirtimeDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task ReadBillPaymentTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=BillPaymentDB;";
        Console.WriteLine("=== BILL PAYMENT TRANSACTIONS ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 10 AccountNumber, Amount, TransactionId, PaymentDate, BillType, Status FROM BillPaymentTransactions WHERE Status = 'SUCCESS' ORDER BY PaymentDate DESC",
                "SELECT TOP 10 AccountNumber, Amount, TransactionId, CreatedDate, TransactionType, Status FROM Transactions WHERE TransactionType LIKE '%BILL%' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC"
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
                        Console.WriteLine($"{"Account",-12} {"Amount",-10} {"Transaction ID",-15} {"Date",-20} {"Bill Type",-15} {"Status",-10}");
                        Console.WriteLine(new string('-', 85));
                        
                        while (await reader.ReadAsync())
                        {
                            var account = reader.GetString(0);
                            var amount = reader.GetDecimal(1);
                            var transactionId = reader.GetString(2);
                            var date = reader.GetDateTime(3);
                            var billType = reader.FieldCount > 4 ? reader.GetString(4) : "N/A";
                            var status = reader.GetString(reader.FieldCount - 1);
                            
                            Console.WriteLine($"{account,-12} {amount,-10:C} {transactionId,-15} {date:yyyy-MM-dd HH:mm,-20} {billType,-15} {status,-10}");
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
                Console.WriteLine("No successful bill payment transactions found.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"BillPaymentDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task ReadTransferTransactions()
    {
        var connectionString = $"{BaseConnectionString};Database=TransferDB;";
        Console.WriteLine("=== TRANSFER TRANSACTIONS ===");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var queries = new[]
            {
                "SELECT TOP 10 FromAccount, Amount, TransactionId, TransferDate, TransferType, Status FROM TransferTransactions WHERE Status = 'SUCCESS' ORDER BY TransferDate DESC",
                "SELECT TOP 10 AccountNumber, Amount, TransactionId, CreatedDate, TransactionType, Status FROM Transactions WHERE TransactionType = 'TRANSFER' AND Status = 'SUCCESS' ORDER BY CreatedDate DESC"
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
                        Console.WriteLine($"{"From Account",-12} {"Amount",-10} {"Transaction ID",-15} {"Date",-20} {"Transfer Type",-15} {"Status",-10}");
                        Console.WriteLine(new string('-', 85));
                        
                        while (await reader.ReadAsync())
                        {
                            var account = reader.GetString(0);
                            var amount = reader.GetDecimal(1);
                            var transactionId = reader.GetString(2);
                            var date = reader.GetDateTime(3);
                            var transferType = reader.FieldCount > 4 ? reader.GetString(4) : "N/A";
                            var status = reader.GetString(reader.FieldCount - 1);
                            
                            Console.WriteLine($"{account,-12} {amount,-10:C} {transactionId,-15} {date:yyyy-MM-dd HH:mm,-20} {transferType,-15} {status,-10}");
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
                Console.WriteLine("No successful transfer transactions found.");
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"TransferDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
}