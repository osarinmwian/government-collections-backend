namespace KeyLoyalty.Domain.Entities;

public class AccountingEntry
{
    public string GLAccount { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}