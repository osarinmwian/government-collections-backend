using GovernmentCollections.Domain.DTOs.RevPay;
using GovernmentCollections.Service.Services.RevPay.BillType;
using GovernmentCollections.Service.Services.RevPay.Payment;
using GovernmentCollections.Service.Services.RevPay.Transaction;

namespace GovernmentCollections.Service.Services.RevPay;

public class RevPayService : IRevPayService
{
    private readonly IRevPayBillTypeService _billTypeService;
    private readonly IRevPayPaymentService _paymentService;
    private readonly IRevPayTransactionService _transactionService;

    public RevPayService(
        IRevPayBillTypeService billTypeService,
        IRevPayPaymentService paymentService,
        IRevPayTransactionService transactionService)
    {
        _billTypeService = billTypeService;
        _paymentService = paymentService;
        _transactionService = transactionService;
    }

    public Task<dynamic> GetBillTypesAsync() => _billTypeService.GetBillTypesAsync();

    public Task<dynamic> ValidateReferenceAsync(RevPayValidateRequest request) => _billTypeService.ValidateReferenceAsync(request);

    public Task<dynamic> ProcessPaymentAsync(RevPayPaymentRequest request) => _paymentService.ProcessPaymentAsync(request);

    public Task<dynamic> GenerateWebGuidAsync(RevPayWebGuidRequest request) => _paymentService.GenerateWebGuidAsync(request);

    public Task<dynamic> VerifyPidAsync(RevPayPidVerificationRequest request) => _billTypeService.VerifyPidAsync(request);

    public Task<dynamic> GetReceiptAsync(RevPayReceiptRequest request) => _paymentService.GetReceiptAsync(request);

    public Task<dynamic> ProcessTransactionWithAuthAsync(RevPayTransactionRequest request) => _transactionService.ProcessTransactionWithAuthAsync(request);
}