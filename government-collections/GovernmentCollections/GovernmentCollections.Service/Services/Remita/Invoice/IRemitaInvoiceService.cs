namespace GovernmentCollections.Service.Services.Remita.Invoice;

public interface IRemitaInvoiceService
{
    Task<RemitaInvoiceResponse> GenerateInvoiceAsync(RemitaInvoiceRequest request);
    Task<RemitaPaymentStatusResponse> VerifyPaymentAsync(string rrr);
}