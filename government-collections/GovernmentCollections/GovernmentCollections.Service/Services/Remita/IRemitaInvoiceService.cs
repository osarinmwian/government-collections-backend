namespace GovernmentCollections.Service.Services.Remita;

public interface IRemitaInvoiceService
{
    Task<RemitaInvoiceResponse> GenerateInvoiceAsync(RemitaInvoiceRequest request);
    Task<RemitaPaymentStatusResponse> VerifyPaymentAsync(string rrr);
}