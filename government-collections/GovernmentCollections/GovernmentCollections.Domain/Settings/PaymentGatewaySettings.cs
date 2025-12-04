namespace GovernmentCollections.Domain.Settings;

public class RevPaySettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class RemitaSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class InterswitchSettings
{
    public string BaseUrl { get; set; } = "https://passport.k8.isw.la";
    public string ServicesUrl { get; set; } = "https://qa.interswitchng.com";
    public string UserName { get; set; } = "IKIA72C65D005F93F30E573EFEAC04FA6DD9E4D344B1";
    public string Password { get; set; } = "YZMqZezsltpSPNb4+49PGeP7lYkzKn1a5SaVSyzKOiI=";
    public string MerchantCode { get; set; } = "QTELL";
    public string RequestorId { get; set; } = "00110919551";
    public string TerminalId { get; set; } = "3PBL0001";
    public string PayableId { get; set; } = "109";
    public string InstitutionId { get; set; } = "12899";
    public int TokenExpiryBuffer { get; set; } = 300; // 5 minutes buffer
}

public class BuyPowerSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}