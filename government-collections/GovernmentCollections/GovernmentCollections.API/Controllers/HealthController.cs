using GovernmentCollections.Data.Context;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentCollections.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IGovernmentCollectionsContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IGovernmentCollectionsContext context,
        ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        return Ok(healthStatus);
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var checks = new Dictionary<string, object>();

        // Database check
        try
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM GovernmentPayments";
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            checks["Database"] = new { Status = "Healthy", RecordCount = count };
        }
        catch (Exception ex)
        {
            checks["Database"] = new { Status = "Unhealthy", Error = ex.Message };
        }

        // API Health
        checks["API"] = new { Status = "Healthy", Message = "API is running" };

        var overallStatus = checks.Values.All(c => ((dynamic)c).Status == "Healthy") ? "Healthy" : "Degraded";

        var healthStatus = new
        {
            Status = overallStatus,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Checks = checks
        };

        return Ok(healthStatus);
    }
}