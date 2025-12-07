using System.Text.Json.Serialization;

namespace GovernmentCollections.Domain.DTOs.Interswitch;

public class InterswitchAuthRequest
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class InterswitchAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("merchant_code")]
    public string MerchantCode { get; set; } = string.Empty;

    [JsonPropertyName("requestor_id")]
    public string RequestorId { get; set; } = string.Empty;

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = string.Empty;

    [JsonPropertyName("payable_id")]
    public string PayableId { get; set; } = string.Empty;

    [JsonPropertyName("institution_id")]
    public string InstitutionId { get; set; } = string.Empty;
}

public class InterswitchBiller
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("Narration")]
    public string Narration { get; set; } = string.Empty;

    [JsonPropertyName("CustomerField1")]
    public string CustomerField1 { get; set; } = string.Empty;

    [JsonPropertyName("CustomerField2")]
    public string CustomerField2 { get; set; } = string.Empty;

    [JsonPropertyName("Surcharge")]
    public string Surcharge { get; set; } = string.Empty;

    [JsonPropertyName("CurrencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("CurrencySymbol")]
    public string CurrencySymbol { get; set; } = string.Empty;

    [JsonPropertyName("CategoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("CategoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("AmountType")]
    public int AmountType { get; set; }
}

public class InterswitchCategory
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Billers")]
    public List<InterswitchBiller> Billers { get; set; } = new();
}

public class InterswitchBillerList
{
    [JsonPropertyName("Count")]
    public int Count { get; set; }

    [JsonPropertyName("Category")]
    public List<InterswitchCategory> Categories { get; set; } = new();
}

public class InterswitchServicesResponse
{
    [JsonPropertyName("BillerList")]
    public InterswitchBillerList BillerList { get; set; } = new();
}

public class InterswitchPaymentRequest
{
    [JsonPropertyName("billerId")]
    public int BillerId { get; set; }

    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("requestReference")]
    public string RequestReference { get; set; } = string.Empty;

    [JsonPropertyName("SecondFa")]
    public string SecondFa { get; set; } = string.Empty;

    [JsonPropertyName("SecondFaType")]
    public string SecondFaType { get; set; } = string.Empty;

    [JsonPropertyName("Channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("Enforce2FA")]
    public bool Enforce2FA { get; set; }

    [JsonPropertyName("Pin")]
    public string Pin { get; set; } = string.Empty;
}

public class InterswitchPaymentResponse
{
    [JsonPropertyName("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    [JsonPropertyName("responseMessage")]
    public string ResponseMessage { get; set; } = string.Empty;

    [JsonPropertyName("transactionReference")]
    public string TransactionReference { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("surcharge")]
    public decimal Surcharge { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("settlementReference")]
    public string SettlementReference { get; set; } = string.Empty;
}

public class InterswitchBillInquiryRequest
{
    [JsonPropertyName("billerId")]
    public int BillerId { get; set; }

    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;
}

public class InterswitchBillInquiryResponse
{
    [JsonPropertyName("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    [JsonPropertyName("responseMessage")]
    public string ResponseMessage { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("billDescription")]
    public string BillDescription { get; set; } = string.Empty;
}

public class InterswitchPaymentItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;
}

public class InterswitchCustomerValidationRequest
{
    [JsonPropertyName("billerId")]
    public int BillerId { get; set; }

    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;

    [JsonPropertyName("paymentCode")]
    public string PaymentCode { get; set; } = string.Empty;
}

public class InterswitchCustomerValidationResponse
{
    [JsonPropertyName("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    [JsonPropertyName("responseMessage")]
    public string ResponseMessage { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("paymentItems")]
    public List<InterswitchPaymentItem> PaymentItems { get; set; } = new();
}

public class InterswitchTransactionHistoryResponse
{
    [JsonPropertyName("transactions")]
    public List<InterswitchTransaction> Transactions { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}

public class InterswitchTransaction
{
    [JsonPropertyName("transactionReference")]
    public string TransactionReference { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("billerName")]
    public string BillerName { get; set; } = string.Empty;

    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("transactionDate")]
    public DateTime TransactionDate { get; set; }
}

public class InterswitchPaymentItemsRequest
{
    [JsonPropertyName("customerReference")]
    public string CustomerReference { get; set; } = string.Empty;
}

public class InterswitchTransactionRequest
{
    [JsonPropertyName("paymentCode")]
    public string PaymentCode { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("customerMobile")]
    public string CustomerMobile { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("requestReference")]
    public string RequestReference { get; set; } = string.Empty;

    [JsonPropertyName("secondFa")]
    public string SecondFa { get; set; } = string.Empty;

    [JsonPropertyName("secondFaType")]
    public string SecondFaType { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("enforce2FA")]
    public bool Enforce2FA { get; set; }

    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
}

public class InterswitchCustomerValidationBatchRequest
{
    [JsonPropertyName("customers")]
    public List<InterswitchCustomerInfo> Customers { get; set; } = new();
}

public class InterswitchCustomerInfo
{
    [JsonPropertyName("PaymentCode")]
    public string PaymentCode { get; set; } = string.Empty;

    [JsonPropertyName("CustomerId")]
    public string CustomerId { get; set; } = string.Empty;
}

public class GovernmentBillersFilter
{
    public List<string> GovernmentCategories { get; set; } = new()
    {
        "State Payments",
        "Tax Payments",
        "Quickteller Business"
    };

    public List<string> GovernmentKeywords { get; set; } = new()
    {
        "government", "tax", "firs", "state", "federal", "ministry", "agency",
        "revenue", "customs", "immigration", "police", "court", "license",
        "permit", "levy", "fee", "fine", "penalty"
    };
}

public static class InterswitchValidationHelper
{
    public static (bool IsValid, string Message) ValidateEnhancedAuthentication(string secondFa, string secondFaType, string channel, bool enforce2FA)
    {
        if (enforce2FA)
        {
            if (string.IsNullOrEmpty(secondFa))
                return (false, "Second factor authentication required when Enforce2FA is true");

            if (string.IsNullOrEmpty(secondFaType))
                return (false, "SecondFaType is required when Enforce2FA is true");

            var validTypes = new[] { "SMS", "EMAIL", "TOTP", "BIOMETRIC", "HARDWARE_TOKEN" };
            if (!validTypes.Contains(secondFaType?.ToUpper()))
                return (false, "Invalid SecondFaType. Valid types: SMS, EMAIL, TOTP, BIOMETRIC, HARDWARE_TOKEN");
        }

        if (!string.IsNullOrEmpty(channel))
        {
            var validChannels = new[] { "MOBILE", "WEB", "USSD", "ATM", "POS", "API" };
            if (!validChannels.Contains(channel?.ToUpper()))
                return (false, "Invalid Channel. Valid channels: MOBILE, WEB, USSD, ATM, POS, API");
        }

        return (true, "Validation successful");
    }
}