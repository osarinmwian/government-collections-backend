using System.Text.Json.Serialization;

namespace GovernmentCollections.Domain.DTOs.Remita;

public class RemitaInitiatePaymentDto
{
    public string ServiceTypeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RemitaPaymentCallbackDto
{
    public string RRR { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RemitaTransactionStatusDto
{
    public string RRR { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
}

public class RemitaRefundDto
{
    public string RRR { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RemitaMandateDto
{
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public string PayerBankCode { get; set; } = string.Empty;
    public string PayerAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string MandateType { get; set; } = string.Empty;
}

public class RemitaDebitMandateDto
{
    public string MandateId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FundingAccount { get; set; } = string.Empty;
    public string FundingBankCode { get; set; } = string.Empty;
    public string DebitDate { get; set; } = string.Empty;
}

public class RemitaCustomerValidationDto
{
    [JsonPropertyName("billPaymentProductId")]
    public string BillPaymentProductId { get; set; } = string.Empty;
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
}

public class RemitaTransactionInitiateDto
{
    [JsonPropertyName("billPaymentProductId")]
    public string BillPaymentProductId { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("transactionRef")]
    public string TransactionRef { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("metadata")]
    public RemitaMetadata? Metadata { get; set; }
    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
    [JsonPropertyName("secondFa")]
    public string SecondFa { get; set; } = string.Empty;
    [JsonPropertyName("secondFaType")]
    public string SecondFaType { get; set; } = string.Empty;
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;
    [JsonPropertyName("enforce2FA")]
    public bool Enforce2FA { get; set; }
}

public class RemitaPaymentProcessDto
{
    [JsonPropertyName("rrr")]
    public string Rrr { get; set; } = string.Empty;
    [JsonPropertyName("transactionRef")]
    public string TransactionRef { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;
    [JsonPropertyName("metadata")]
    public RemitaPaymentMetadata? Metadata { get; set; }
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
    [JsonIgnore]
    public string Pin { get; set; } = string.Empty;
    [JsonPropertyName("secondFa")]
    public string SecondFa { get; set; } = string.Empty;
    [JsonPropertyName("secondFaType")]
    public string SecondFaType { get; set; } = string.Empty;
    [JsonPropertyName("enforce2FA")]
    public bool Enforce2FA { get; set; }
}

public class RemitaMetadata
{
    [JsonPropertyName("customFields")]
    public List<RemitaCustomField>? CustomFields { get; set; }
}

public class RemitaPaymentMetadata
{
    [JsonPropertyName("fundingSource")]
    public string? FundingSource { get; set; }
    [JsonPropertyName("payerAccountNumber")]
    public string? PayerAccountNumber { get; set; }
}

public class RemitaCustomField
{
    [JsonPropertyName("variable_name")]
    public string VariableName { get; set; } = string.Empty;
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

// Simplified API Response DTOs
public class RemitaBillerDto
{
    public string BillerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class RemitaBillerDetailsDto
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RemitaBillerProductsData Data { get; set; } = new();
}

public class RemitaValidateCustomerRequest
{
    [JsonPropertyName("billPaymentProductId")]
    public string BillPaymentProductId { get; set; } = string.Empty;
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
}

public class RemitaValidateCustomerResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RemitaCustomerValidationData? Data { get; set; }
}

public class RemitaPaymentRequest
{
    public string BillerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TransactionRef { get; set; } = string.Empty;
}

public class RemitaPaymentResponse
{
    public string Status { get; set; } = string.Empty;
    public string Rrr { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
    public string ReceiptUrl { get; set; } = string.Empty;
}

// Internal Remita API Response DTOs
public class RemitaAuthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public List<RemitaTokenData> Data { get; set; } = new();
}

public class RemitaTokenData
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

public class RemitaBillersResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public List<RemitaBillerData> Data { get; set; } = new();
}

public class RemitaBillerData
{
    [JsonPropertyName("billerId")]
    public string BillerId { get; set; } = string.Empty;
    [JsonPropertyName("billerName")]
    public string BillerName { get; set; } = string.Empty;
    [JsonPropertyName("billerShortName")]
    public string BillerShortName { get; set; } = string.Empty;
    [JsonPropertyName("billerLogoUrl")]
    public string? BillerLogoUrl { get; set; }
    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("categoryDescription")]
    public string CategoryDescription { get; set; } = string.Empty;
}

public class RemitaBillerProductsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public RemitaBillerProductsData Data { get; set; } = new();
}

public class RemitaBillerProductsData
{
    [JsonPropertyName("billerId")]
    public string BillerId { get; set; } = string.Empty;
    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;
    [JsonPropertyName("products")]
    public List<RemitaProductData> Products { get; set; } = new();
}

public class RemitaProductData
{
    [JsonPropertyName("billPaymentProductName")]
    public string BillPaymentProductName { get; set; } = string.Empty;
    [JsonPropertyName("billPaymentProductId")]
    public string BillPaymentProductId { get; set; } = string.Empty;
    [JsonPropertyName("isAmountFixed")]
    public bool IsAmountFixed { get; set; }
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("metadata")]
    public RemitaProductMetadata Metadata { get; set; } = new();
    [JsonPropertyName("posEnabled")]
    public bool? PosEnabled { get; set; }
    [JsonPropertyName("posOptimized")]
    public bool? PosOptimized { get; set; }
}

public class RemitaProductMetadata
{
    [JsonPropertyName("customFields")]
    public List<RemitaProductCustomField> CustomFields { get; set; } = new();
}

public class RemitaProductCustomField
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("variable_name")]
    public string VariableName { get; set; } = string.Empty;
    [JsonPropertyName("validation")]
    public bool Validation { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("required")]
    public bool Required { get; set; }
    [JsonPropertyName("sortOrder")]
    public string? SortOrder { get; set; }
    [JsonPropertyName("variableId")]
    public long VariableId { get; set; }
    [JsonPropertyName("selectOptions")]
    public List<RemitaSelectOption> SelectOptions { get; set; } = new();
}

public class RemitaSelectOption
{
    [JsonPropertyName("VALUE")]
    public string Value { get; set; } = string.Empty;
    [JsonPropertyName("DISPLAY_NAME")]
    public string DisplayName { get; set; } = string.Empty;
}

public class RemitaCustomerValidationResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public RemitaCustomerValidationData? Data { get; set; }
}

public class RemitaCustomerValidationData
{
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;
    [JsonPropertyName("billPaymentProductId")]
    public string BillPaymentProductId { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
    [JsonPropertyName("maximumAmount")]
    public decimal? MaximumAmount { get; set; }
    [JsonPropertyName("minimumAmount")]
    public decimal? MinimumAmount { get; set; }
    [JsonPropertyName("valueList")]
    public List<object> ValueList { get; set; } = new();
}

public class RemitaTransactionInitResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class RemitaPaymentProcessResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class RemitaTransactionQueryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public RemitaTransactionQueryData? Data { get; set; }
}

public class RemitaTransactionQueryData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("rrr")]
    public string Rrr { get; set; } = string.Empty;
    [JsonPropertyName("paid")]
    public bool Paid { get; set; }
    [JsonPropertyName("metadata")]
    public RemitaTransactionMetadata? Metadata { get; set; }
}

public class RemitaTransactionMetadata
{
    [JsonPropertyName("receiptUrl")]
    public string? ReceiptUrl { get; set; }
}

public class RemitaInvoiceRequest
{
    public string ServiceTypeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RemitaRrrPaymentRequest
{
    [JsonPropertyName("rrr")]
    public string Rrr { get; set; } = string.Empty;
    [JsonPropertyName("transactionRef")]
    public string TransactionRef { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "internetbanking";
    [JsonPropertyName("metadata")]
    public RemitaRrrMetadata? Metadata { get; set; }
}

public class RemitaRrrMetadata
{
    [JsonPropertyName("fundingSource")]
    public string? FundingSource { get; set; }
    [JsonPropertyName("payerAccountNumber")]
    public string? PayerAccountNumber { get; set; }
}