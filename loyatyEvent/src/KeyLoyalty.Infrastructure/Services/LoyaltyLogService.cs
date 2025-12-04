using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KeyLoyalty.Infrastructure.Services
{
    public interface ILoyaltyLogService
    {
        Task<List<LogEntry>> GetInboundLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);
        Task<List<LogEntry>> GetOutboundLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);
        Task<List<LogEntry>> GetAllLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);
        Task LogInboundRequestAsync(string method, string path, object? headers, object? body, string? correlationId = null);
        Task LogOutboundResponseAsync(int statusCode, object? headers, object? body, string? correlationId = null);
    }

    public class LogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Direction { get; set; } = string.Empty; // INBOUND, OUTBOUND
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public object? Headers { get; set; }
        public object? Body { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Source { get; set; } = "KeyLoyalty";
    }

    public class LoyaltyLogService : ILoyaltyLogService
    {
        private readonly ILogger<LoyaltyLogService> _logger;
        private static readonly List<LogEntry> _logEntries = new();
        private static readonly object _lock = new();
        private const int MAX_LOG_ENTRIES = 10000;

        public LoyaltyLogService(ILogger<LoyaltyLogService> logger)
        {
            _logger = logger;
        }

        public Task<List<LogEntry>> GetInboundLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            lock (_lock)
            {
                var query = _logEntries.Where(x => x.Direction == "INBOUND");
                
                if (fromDate.HasValue)
                    query = query.Where(x => x.Timestamp >= fromDate.Value);
                
                if (toDate.HasValue)
                    query = query.Where(x => x.Timestamp <= toDate.Value);

                return Task.FromResult(query
                    .OrderByDescending(x => x.Timestamp)
                    .Take(Math.Min(limit, 1000))
                    .ToList());
            }
        }

        public Task<List<LogEntry>> GetOutboundLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            lock (_lock)
            {
                var query = _logEntries.Where(x => x.Direction == "OUTBOUND");
                
                if (fromDate.HasValue)
                    query = query.Where(x => x.Timestamp >= fromDate.Value);
                
                if (toDate.HasValue)
                    query = query.Where(x => x.Timestamp <= toDate.Value);

                return Task.FromResult(query
                    .OrderByDescending(x => x.Timestamp)
                    .Take(Math.Min(limit, 1000))
                    .ToList());
            }
        }

        public Task<List<LogEntry>> GetAllLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            lock (_lock)
            {
                var query = _logEntries.AsQueryable();
                
                if (fromDate.HasValue)
                    query = query.Where(x => x.Timestamp >= fromDate.Value);
                
                if (toDate.HasValue)
                    query = query.Where(x => x.Timestamp <= toDate.Value);

                return Task.FromResult(query
                    .OrderByDescending(x => x.Timestamp)
                    .Take(Math.Min(limit, 1000))
                    .ToList());
            }
        }

        public Task LogInboundRequestAsync(string method, string path, object? headers, object? body, string? correlationId = null)
        {
            var logEntry = new LogEntry
            {
                Direction = "INBOUND",
                Method = method,
                Path = path,
                Headers = headers,
                Body = body,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString()
            };

            AddLogEntry(logEntry);
            
            _logger.LogInformation("INBOUND_LOG: {Method} {Path} | CorrelationId: {CorrelationId} | Body: {Body}",
                method, path, correlationId, JsonSerializer.Serialize(body));

            return Task.CompletedTask;
        }

        public Task LogOutboundResponseAsync(int statusCode, object? headers, object? body, string? correlationId = null)
        {
            var logEntry = new LogEntry
            {
                Direction = "OUTBOUND",
                StatusCode = statusCode,
                Headers = headers,
                Body = body,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString()
            };

            AddLogEntry(logEntry);
            
            _logger.LogInformation("OUTBOUND_LOG: {StatusCode} | CorrelationId: {CorrelationId} | Body: {Body}",
                statusCode, correlationId, JsonSerializer.Serialize(body));

            return Task.CompletedTask;
        }

        private void AddLogEntry(LogEntry entry)
        {
            lock (_lock)
            {
                _logEntries.Add(entry);
                
                // Keep only the most recent entries to prevent memory issues
                if (_logEntries.Count > MAX_LOG_ENTRIES)
                {
                    var toRemove = _logEntries.Count - MAX_LOG_ENTRIES;
                    _logEntries.RemoveRange(0, toRemove);
                }
            }
        }
    }
}