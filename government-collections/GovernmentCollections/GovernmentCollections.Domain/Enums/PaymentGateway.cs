namespace GovernmentCollections.Domain.Enums;

public enum PaymentGateway
{
    RevPay = 1,
    Remita = 2,
    Interswitch = 3,
    BuyPower = 4
}

public enum PaymentType
{
    Tax = 1,
    Levy = 2,
    License = 3,
    StatutoryFee = 4,
    VehicleLicense = 5,
    BusinessPermit = 6
}

public enum TransactionStatus
{
    Pending = 1,
    Successful = 2,
    Failed = 3,
    Cancelled = 4
}