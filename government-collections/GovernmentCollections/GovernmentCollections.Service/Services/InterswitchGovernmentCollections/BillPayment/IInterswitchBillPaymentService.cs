using GovernmentCollections.Domain.DTOs.Interswitch;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.BillPayment;

public interface IInterswitchBillPaymentService
{
    Task<List<InterswitchBiller>> GetGovernmentBillersAsync();
    Task<List<InterswitchBiller>> GetBillersByCategoryAsync(int categoryId);
    Task<List<InterswitchCategory>> GetGovernmentCategoriesAsync();
    Task<List<InterswitchPaymentItem>> GetServiceOptionsAsync(int serviceId);
}