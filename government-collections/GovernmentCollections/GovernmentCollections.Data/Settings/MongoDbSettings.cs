namespace GovernmentCollections.Data.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string PaymentsCollectionName { get; set; } = "GovernmentPayments";
}