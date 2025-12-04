using KeyLoyalty.Application.Services;
using KeyLoyalty.Domain.Events;
using KeyLoyalty.Infrastructure.Events;
using KeyLoyalty.Infrastructure.Repositories;
using KeyLoyalty.Infrastructure.Services;
using KeyLoyalty.API.Middleware;
using Serilog;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Loyalty event channel for listening to banking system transactions
var loyaltyChannel = Channel.CreateUnbounded<LoyaltyTransactionEvent>();
builder.Services.AddSingleton(loyaltyChannel.Reader);
builder.Services.AddSingleton(loyaltyChannel.Writer);

var baseEventChannel = Channel.CreateUnbounded<KeyLoyalty.Domain.Events.BaseEvent>();
builder.Services.AddSingleton<ChannelReader<LoyaltyTransactionEvent>>(loyaltyChannel.Reader);
builder.Services.AddSingleton<ChannelWriter<LoyaltyTransactionEvent>>(loyaltyChannel.Writer);
builder.Services.AddSingleton<ChannelWriter<KeyLoyalty.Domain.Events.BaseEvent>>(baseEventChannel.Writer);

builder.Services.AddScoped<KeyLoyalty.Infrastructure.Repositories.ICustomerLoyaltyRepository, KeyLoyalty.Infrastructure.Repositories.CustomerLoyaltyRepository>();
builder.Services.AddScoped<KeyLoyalty.Infrastructure.Events.IEventPublisher, KeyLoyalty.Infrastructure.Events.EventPublisher>();
builder.Services.AddScoped<KeyLoyalty.Application.Services.ILoyaltyApplicationService, KeyLoyalty.Application.Services.LoyaltyApplicationService>();
builder.Services.AddScoped<ILoyaltyEventPublisher, LoyaltyEventPublisher>();
builder.Services.AddHostedService<LoyaltyEventListener>();

// Transaction services
builder.Services.AddScoped<IProcessedTransactionService, ProcessedTransactionService>();
builder.Services.AddScoped<ITransactionReaderService, TransactionReaderService>();
builder.Services.AddScoped<ITransactionLoggerService, TransactionLoggerService>();
builder.Services.AddScoped<KeyLoyalty.Infrastructure.Services.IAccountingService, KeyLoyalty.Infrastructure.Services.AccountingService>();
builder.Services.AddScoped<KeyLoyalty.Infrastructure.Services.IAccountMappingService, KeyLoyalty.Infrastructure.Services.AccountMappingService>();
builder.Services.AddScoped<KeyLoyalty.Infrastructure.Services.IPaymentService, KeyLoyalty.Infrastructure.Services.PaymentService>();
builder.Services.AddHostedService<TransactionPollingService>();
builder.Services.AddHostedService<PointExpiryBackgroundService>();

// Logging services
builder.Services.AddSingleton<ILoyaltyLogService, LoyaltyLogService>();

// Alert services
builder.Services.AddScoped<KeyLoyalty.Infrastructure.Services.IAlertService, KeyLoyalty.Infrastructure.Services.AlertService>();

builder.Services.AddHostedService<KeyLoyaltyHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseHealthChecks("/health");
app.UseHealthChecks("/health/ready");
app.UseHealthChecks("/health/live");
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("KeyLoyalty API starting on {Urls}", app.Configuration["Urls"]);
app.Logger.LogInformation("Swagger UI available at: {SwaggerUrl}", "http://localhost:5000/swagger");

app.Run();