using GovernmentCollections.Domain.DTOs.Remita;
using GovernmentCollections.Service.Services.Remita.BillPayment;
using GovernmentCollections.Service.Services.Remita.Payment;
using GovernmentCollections.Service.Services.Remita.Transaction;

namespace GovernmentCollections.Service.Services.Remita;

public class RemitaService : IRemitaService
{
    private readonly IRemitaBillPaymentService _billPaymentService;
    private readonly IRemitaPaymentService _paymentService;
    private readonly IRemitaTransactionService _transactionService;

    public RemitaService(
        IRemitaBillPaymentService billPaymentService,
        IRemitaPaymentService paymentService,
        IRemitaTransactionService transactionService)
    {
        _billPaymentService = billPaymentService;
        _paymentService = paymentService;
        _transactionService = transactionService;
    }

    public Task<List<RemitaBillerDto>> GetBillersAsync() => _billPaymentService.GetBillersAsync();

    public Task<RemitaBillerDetailsDto> GetBillerByIdAsync(string billerId) => _billPaymentService.GetBillerByIdAsync(billerId);

    public Task<RemitaValidateCustomerResponse> ValidateCustomerAsync(RemitaValidateCustomerRequest request) => _billPaymentService.ValidateCustomerAsync(request);

    public Task<RemitaPaymentResponse> ProcessPaymentAsync(RemitaPaymentRequest request) => _paymentService.ProcessPaymentAsync(request);

    public Task<dynamic> ProcessTransactionWithAuthAsync(RemitaTransactionInitiateDto request) => _transactionService.ProcessTransactionWithAuthAsync(request);

    public Task<dynamic> InitiatePaymentAsync(RemitaInitiatePaymentDto request) => _paymentService.InitiatePaymentAsync(request);

    public Task<dynamic> VerifyPaymentAsync(string rrr) => _paymentService.VerifyPaymentAsync(rrr);

    public Task<dynamic> GetTransactionStatusAsync(string transactionId) => _transactionService.GetTransactionStatusAsync(transactionId);

    public Task<dynamic> GetActiveBanksAsync() => _paymentService.GetActiveBanksAsync();

    public Task<dynamic> ActivateMandateAsync(RemitaRrrPaymentRequest request) => _paymentService.ActivateMandateAsync(request);

    public Task<dynamic> GetRrrDetailsAsync(string rrr) => _paymentService.GetRrrDetailsAsync(rrr);

    public Task<dynamic> ActivateRrrPaymentAsync(RemitaRrrPaymentRequest request) => _paymentService.ActivateRrrPaymentAsync(request);

    public Task<dynamic> ProcessRrrPaymentAsync(RemitaRrrPaymentRequest request) => _paymentService.ProcessRrrPaymentAsync(request);
}