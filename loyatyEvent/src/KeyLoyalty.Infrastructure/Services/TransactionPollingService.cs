using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Services
{
    public class TransactionPollingService : BackgroundService
    {
        private readonly ITransactionReaderService _transactionReader;
        private readonly ITransactionLoggerService _transactionLogger;
        private readonly ILogger<TransactionPollingService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

        public TransactionPollingService(
            ITransactionReaderService transactionReader,
            ITransactionLoggerService transactionLogger,
            ILogger<TransactionPollingService> logger)
        {
            _transactionReader = transactionReader;
            _transactionLogger = transactionLogger;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction Polling Service started with duplicate protection");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🔄 REALTIME_POLLING: Starting real-time transaction scan at {Timestamp}", DateTime.Now);

                    // Log all transactions in real-time
                    await _transactionLogger.LogAllTransactions();

                    // Process loyalty events with duplicate protection
                    await _transactionReader.ProcessAirtimeTransactions();
                    await _transactionReader.ProcessBillPaymentTransactions();
                    await _transactionReader.ProcessTransferTransactions();

                    _logger.LogInformation("REALTIME_COMPLETE: Real-time scan completed at {Timestamp}", DateTime.Now);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("ProcessedTransactions table missing"))
                {
                    _logger.LogError("CRITICAL: ProcessedTransactions table missing. Service will retry in 5 minutes. Run CreateProcessedTransactionsTable.sql!");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during transaction polling: {Message}", ex.Message);
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Transaction Polling Service stopped");
        }
    }
}