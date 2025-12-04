using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "Server=10.40.14.22,1433;Database=OmniChannelDB2;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;";
        
        Console.WriteLine("Testing KeystoneOmniTransactions table access...\n");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("✅ Connected to OmniChannelDB2 successfully");
            
            // Test Airtime transactions
            await TestAirtimeTransactions(connection);
            
            // Test Bill Payment transactions  
            await TestBillPaymentTransactions(connection);
            
            // Test Transfer transactions
            await TestTransferTransactions(connection);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
    
    static async Task TestAirtimeTransactions(SqlConnection connection)
    {
        Console.WriteLine("\n📱 Testing Airtime/Data Transactions:");
        var sql = @"SELECT TOP 3 Draccount, Amount, Requestid, transactiondate, Transactiontype, Usernetwork 
                   FROM KeystoneOmniTransactions 
                   WHERE (Transactiontype = 'Airtime' OR Transactiontype = 'MobileData') 
                   AND Txnstatus = '00' 
                   ORDER BY transactiondate DESC";
        
        var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Console.WriteLine($"  Account: {reader.GetString(0)}, Amount: {reader.GetDecimal(1)}, Type: {reader.GetString(4)}, Date: {reader.GetDateTime(3)}");
        }
        Console.WriteLine($"  Found {count} airtime/data transactions");
    }
    
    static async Task TestBillPaymentTransactions(SqlConnection connection)
    {
        Console.WriteLine("\n💡 Testing Bill Payment Transactions:");
        var sql = @"SELECT TOP 3 Draccount, Amount, Requestid, transactiondate, Billername, Billerproduct 
                   FROM KeystoneOmniTransactions 
                   WHERE Transactiontype = 'BillsPayment' 
                   AND Txnstatus = '00' 
                   ORDER BY transactiondate DESC";
        
        var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var biller = !reader.IsDBNull(4) ? reader.GetString(4) : "N/A";
            Console.WriteLine($"  Account: {reader.GetString(0)}, Amount: {reader.GetDecimal(1)}, Biller: {biller}, Date: {reader.GetDateTime(3)}");
        }
        Console.WriteLine($"  Found {count} bill payment transactions");
    }
    
    static async Task TestTransferTransactions(SqlConnection connection)
    {
        Console.WriteLine("\n💸 Testing Transfer Transactions:");
        var sql = @"SELECT TOP 3 Draccount, Amount, Requestid, transactiondate, Transactiontype, Craccount 
                   FROM KeystoneOmniTransactions 
                   WHERE Transactiontype IN ('NIP', 'Internal', 'OwnInternal', 'InterBank', 'NQR') 
                   AND Txnstatus = '00' 
                   ORDER BY transactiondate DESC";
        
        var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var creditAccount = !reader.IsDBNull(5) ? reader.GetString(5) : "N/A";
            Console.WriteLine($"  Account: {reader.GetString(0)}, Amount: {reader.GetDecimal(1)}, Type: {reader.GetString(4)}, Date: {reader.GetDateTime(3)}");
        }
        Console.WriteLine($"  Found {count} transfer transactions");
    }
}