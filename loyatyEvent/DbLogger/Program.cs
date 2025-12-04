using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

class Program
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
            await LogOmniChannelDatabase();
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
        try
        {
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
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"KeyLoyaltyDB not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
    
    static async Task LogOmniChannelDatabase()
    {
        var connectionString = $"{BaseConnectionString};Database=OmniChannelDB2;";
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("=== OMNICHANNEL DATABASE (OmniChannelDB2) ===");
            
            // Check for transaction tables
            var tableQueries = new[]
            {
                ("Airtime Transactions", "SELECT TOP 10 * FROM AirtimeTransactions WHERE Status = 'SUCCESS' ORDER BY TransactionDate DESC"),
                ("Bill Payment Transactions", "SELECT TOP 10 * FROM BillPaymentTransactions WHERE Status = 'SUCCESS' ORDER BY PaymentDate DESC"),
                ("Transfer Transactions", "SELECT TOP 10 * FROM TransferTransactions WHERE Status = 'SUCCESS' ORDER BY TransferDate DESC"),
                ("General Transactions", "SELECT TOP 10 * FROM Transactions WHERE Status = 'SUCCESS' ORDER BY CreatedDate DESC"),
                ("Customer Loyalty", "SELECT * FROM CustomerLoyalty ORDER BY LastUpdated DESC")
            };
            
            foreach (var (tableName, query) in tableQueries)
            {
                try
                {
                    var command = new SqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    
                    Console.WriteLine($"\n{tableName}:");
                    Console.WriteLine(new string('-', tableName.Length + 1));
                    
                    if (!reader.HasRows)
                    {
                        Console.WriteLine($"No records found in {tableName}.");
                        continue;
                    }
                    
                    // Print column headers
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write($"{reader.GetName(i),-15} ");
                    }
                    Console.WriteLine();
                    Console.WriteLine(new string('-', reader.FieldCount * 16));
                    
                    int rowCount = 0;
                    while (await reader.ReadAsync() && rowCount < 5)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                            var displayValue = value?.Length > 14 ? value.Substring(0, 14) : value;
                            Console.Write($"{displayValue,-15} ");
                        }
                        Console.WriteLine();
                        rowCount++;
                    }
                }
                catch (SqlException)
                {
                    Console.WriteLine($"\n{tableName}: Table not found or query failed.");
                }
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"OmniChannelDB2 not accessible: {ex.Message}");
        }
        Console.WriteLine();
    }
}