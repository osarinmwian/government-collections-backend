using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KeyLoyalty.Domain.Events;
using KeyLoyalty.Infrastructure.Events;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace KeyLoyalty.Infrastructure.Services
{
    public interface ITransactionReaderService
    {
        Task ProcessAirtimeTransactions();
        Task ProcessBillPaymentTransactions();
        Task ProcessTransferTransactions();
    }

    public class TransactionReaderService : ITransactionReaderService
    {
        private readonly ILogger<TransactionReaderService> _logger;
        private readonly ILoyaltyEventPublisher _eventPublisher;
        private readonly IConfiguration _configuration;
        private readonly IProcessedTransactionService _processedTransactionService;

        public TransactionReaderService(
            IConfiguration configuration, 
            ILogger<TransactionReaderService> logger,
            ILoyaltyEventPublisher eventPublisher,
            IProcessedTransactionService processedTransactionService)
        {
            _configuration = configuration;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _processedTransactionService = processedTransactionService;
        }

        public async Task ProcessAirtimeTransactions()
        {
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT Draccount, Amount, Requestid, transactiondate, Username, Usernetwork 
                           FROM KeystoneOmniTransactions 
                           WHERE (Transactiontype = 'Airtime' OR Transactiontype = 'MobileData') 
                           AND Txnstatus = '00' 
                           AND transactiondate > DATEADD(MINUTE, -30, GETDATE()) 
                           ORDER BY transactiondate DESC";
                
                _logger.LogDebug("QUERY_AIRTIME: Executing SQL: {SQL}", sql);
                
                var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var transactionCount = 0;
                
                while (await reader.ReadAsync())
                {
                    transactionCount++;
                    var transactionId = reader.GetString(2);
                    
                    var rawTransaction = new
                    {
                        DebitAccount = reader.GetString(0),
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        TransactionDate = reader.GetDateTime(3),
                        Username = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                        Network = !reader.IsDBNull(5) ? reader.GetString(5) : "",
                        TransactionType = "Airtime/MobileData",
                        Status = "SUCCESS",
                        Direction = "OUTBOUND",
                        ProcessedBefore = await _processedTransactionService.IsTransactionProcessedAsync(transactionId)
                    };
                    
                    _logger.LogInformation("RAW_AIRTIME_TXN: {Transaction}", JsonSerializer.Serialize(rawTransaction));
                    
                    if (await _processedTransactionService.IsTransactionProcessedAsync(transactionId))
                    {
                        _logger.LogDebug("SKIP_DUPLICATE: Transaction {TxnId} already processed", transactionId);
                        continue;
                    }
                        
                    var airtimeEvent = new LoyaltyAirtimeEvent
                    {
                        AccountNumber = reader.GetString(0),
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        Timestamp = reader.GetDateTime(3),
                        PhoneNumber = "",
                        Network = !reader.IsDBNull(5) ? reader.GetString(5) : ""
                    };
                    
                    await _processedTransactionService.MarkTransactionAsProcessedAsync(transactionId);
                    await _eventPublisher.PublishAirtimeEventAsync(airtimeEvent);
                    
                    _logger.LogInformation("LOYALTY_EVENT_PUBLISHED: Airtime event for account {Account}, amount {Amount}, network {Network}, txnId {TxnId}", 
                        airtimeEvent.AccountNumber, airtimeEvent.Amount, airtimeEvent.Network, transactionId);
                }
                
                _logger.LogInformation("AIRTIME_PROCESSING_COMPLETE: Processed {Count} transactions", transactionCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DATABASE_ERROR_AIRTIME: Error reading airtime transactions from KeystoneOmniTransactions: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERAL_ERROR_AIRTIME: Unexpected error processing airtime transactions: {Message}", ex.Message);
            }
        }

        public async Task ProcessBillPaymentTransactions()
        {
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT Draccount, Amount, Requestid, transactiondate, Billername, Billerproduct 
                           FROM KeystoneOmniTransactions 
                           WHERE Transactiontype = 'BillsPayment' 
                           AND Txnstatus = '00' 
                           AND transactiondate > DATEADD(MINUTE, -30, GETDATE()) 
                           ORDER BY transactiondate DESC";
                
                _logger.LogDebug("QUERY_BILLPAYMENT: Executing SQL: {SQL}", sql);
                
                var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var transactionCount = 0;
                
                while (await reader.ReadAsync())
                {
                    transactionCount++;
                    var transactionId = reader.GetString(2);
                    
                    var rawTransaction = new
                    {
                        DebitAccount = reader.GetString(0),
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        TransactionDate = reader.GetDateTime(3),
                        BillerName = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                        BillerProduct = !reader.IsDBNull(5) ? reader.GetString(5) : "",
                        TransactionType = "BillsPayment",
                        Status = "SUCCESS",
                        Direction = "OUTBOUND",
                        ProcessedBefore = await _processedTransactionService.IsTransactionProcessedAsync(transactionId)
                    };
                    
                    _logger.LogInformation("RAW_BILLPAYMENT_TXN: {Transaction}", JsonSerializer.Serialize(rawTransaction));
                    
                    if (await _processedTransactionService.IsTransactionProcessedAsync(transactionId))
                    {
                        _logger.LogDebug("SKIP_DUPLICATE: Transaction {TxnId} already processed", transactionId);
                        continue;
                    }
                        
                    var billPaymentEvent = new LoyaltyBillPaymentEvent
                    {
                        AccountNumber = reader.GetString(0),
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        Timestamp = reader.GetDateTime(3),
                        BillType = !reader.IsDBNull(5) ? reader.GetString(5) : "BILL_PAYMENT",
                        BillerName = !reader.IsDBNull(4) ? reader.GetString(4) : ""
                    };
                    
                    await _processedTransactionService.MarkTransactionAsProcessedAsync(transactionId);
                    await _eventPublisher.PublishBillPaymentEventAsync(billPaymentEvent);
                    
                    _logger.LogInformation("LOYALTY_EVENT_PUBLISHED: Bill payment event for account {Account}, amount {Amount}, biller {Biller}, txnId {TxnId}", 
                        billPaymentEvent.AccountNumber, billPaymentEvent.Amount, billPaymentEvent.BillerName, transactionId);
                }
                
                _logger.LogInformation("BILLPAYMENT_PROCESSING_COMPLETE: Processed {Count} transactions", transactionCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DATABASE_ERROR_BILLPAYMENT: Error reading bill payment transactions from KeystoneOmniTransactions: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERAL_ERROR_BILLPAYMENT: Unexpected error processing bill payment transactions: {Message}", ex.Message);
            }
        }

        public async Task ProcessTransferTransactions()
        {
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT Draccount, Amount, Requestid, transactiondate, Transactiontype, Craccount 
                           FROM KeystoneOmniTransactions 
                           WHERE Transactiontype IN ('NIP', 'Internal', 'OwnInternal', 'InterBank', 'NQR') 
                           AND Txnstatus = '00' 
                           AND transactiondate > DATEADD(MINUTE, -30, GETDATE()) 
                           ORDER BY transactiondate DESC";
                
                _logger.LogDebug("QUERY_TRANSFER: Executing SQL: {SQL}", sql);
                
                var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var transactionCount = 0;
                
                while (await reader.ReadAsync())
                {
                    transactionCount++;
                    var transactionId = reader.GetString(2);
                    
                    var rawTransaction = new
                    {
                        DebitAccount = reader.GetString(0),
                        CreditAccount = !reader.IsDBNull(5) ? reader.GetString(5) : "",
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        TransactionDate = reader.GetDateTime(3),
                        TransactionType = reader.GetString(4),
                        Status = "SUCCESS",
                        Direction = "OUTBOUND",
                        ProcessedBefore = await _processedTransactionService.IsTransactionProcessedAsync(transactionId)
                    };
                    
                    _logger.LogInformation("RAW_TRANSFER_TXN: {Transaction}", JsonSerializer.Serialize(rawTransaction));
                    
                    if (await _processedTransactionService.IsTransactionProcessedAsync(transactionId))
                    {
                        _logger.LogDebug("SKIP_DUPLICATE: Transaction {TxnId} already processed", transactionId);
                        continue;
                    }
                        
                    var transferEvent = new LoyaltyTransferEvent
                    {
                        AccountNumber = reader.GetString(0),
                        Amount = reader.GetDecimal(1),
                        TransactionId = transactionId,
                        Timestamp = reader.GetDateTime(3),
                        TransactionType = reader.GetString(4),
                        DebitAccount = reader.GetString(0),
                        CreditAccount = !reader.IsDBNull(5) ? reader.GetString(5) : ""
                    };
                    
                    await _processedTransactionService.MarkTransactionAsProcessedAsync(transactionId);
                    await _eventPublisher.PublishTransferEventAsync(transferEvent);
                    
                    _logger.LogInformation("LOYALTY_EVENT_PUBLISHED: Transfer event for account {Account}, amount {Amount}, type {Type}, txnId {TxnId}", 
                        transferEvent.AccountNumber, transferEvent.Amount, transferEvent.TransactionType, transactionId);
                }
                
                _logger.LogInformation("TRANSFER_PROCESSING_COMPLETE: Processed {Count} transactions", transactionCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DATABASE_ERROR_TRANSFER: Error reading transfer transactions from KeystoneOmniTransactions: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERAL_ERROR_TRANSFER: Unexpected error processing transfer transactions: {Message}", ex.Message);
            }
        }
    }
}