using System.Text.Json.Serialization;

namespace GovernmentCollections.Domain.DTOs.RevPay;

public class RevPayBillTypeRequest
{
    [JsonPropertyName("State")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("Hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "164815029028082";
}

public class RevPayValidateRequest
{
    [JsonPropertyName("WebGuid")]
    public string WebGuid { get; set; } = string.Empty;
    [JsonPropertyName("State")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("Date")]
    public string? Date { get; set; }
    [JsonPropertyName("Hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("Clientid")]
    public string ClientId { get; set; } = "164815029028082";
    [JsonPropertyName("TellerID")]
    public string? TellerId { get; set; }
    [JsonPropertyName("Currency")]
    public string Currency { get; set; } = "NGN";
    [JsonPropertyName("CbnCode")]
    public string? CbnCode { get; set; }
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "LUC";
}

public class RevPayPaymentRequest
{
    [JsonPropertyName("webguid")]
    public string WebGuid { get; set; } = string.Empty;
    [JsonPropertyName("amountpaid")]
    public string AmountPaid { get; set; } = string.Empty;
    [JsonPropertyName("paymentref")]
    public string PaymentRef { get; set; } = string.Empty;
    [JsonPropertyName("creditaccount")]
    public string CreditAccount { get; set; } = string.Empty;
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
    [JsonPropertyName("PaymentChannel")]
    public string PaymentChannel { get; set; } = "eChannel";
    [JsonPropertyName("TellerName")]
    public string TellerName { get; set; } = "TestUser";
    [JsonPropertyName("BankNote")]
    public string BankNote { get; set; } = "Cash";
    [JsonPropertyName("tellerID")]
    public string TellerId { get; set; } = "2038978";
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("clientid")]
    public string ClientId { get; set; } = "164815029028082";
}

public class RevPayWebGuidRequest
{
    [JsonPropertyName("Pid")]
    public string Pid { get; set; } = string.Empty;
    [JsonPropertyName("State")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("Amount")]
    public string Amount { get; set; } = string.Empty;
    [JsonPropertyName("AgencyCode")]
    public string AgencyCode { get; set; } = string.Empty;
    [JsonPropertyName("RevCode")]
    public string RevCode { get; set; } = string.Empty;
    [JsonPropertyName("AppliedDate")]
    public string AppliedDate { get; set; } = string.Empty;
    [JsonPropertyName("AssessmentRef")]
    public string AssessmentRef { get; set; } = string.Empty;
    [JsonPropertyName("Hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "164815029028082";
}

public class RevPayPidVerificationRequest
{
    [JsonPropertyName("pid")]
    public string Pid { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("clientid")]
    public string ClientId { get; set; } = "164815029028082";
}

public class RevPayReceiptRequest
{
    [JsonPropertyName("PaymentRef")]
    public string PaymentRef { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; set; } = "XXSG";
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "164815029028082";
}

public class RevPayTransactionRequest
{
    [JsonPropertyName("pid")]
    public string Pid { get; set; } = string.Empty;
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
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;
    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
    [JsonPropertyName("enforce2FA")]
    public bool Enforce2FA { get; set; }
    [JsonPropertyName("secondFa")]
    public string SecondFa { get; set; } = string.Empty;
    [JsonPropertyName("secondFaType")]
    public string SecondFaType { get; set; } = string.Empty;
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;
}