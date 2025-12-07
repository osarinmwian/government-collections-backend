namespace GovernmentCollections.Domain.DTOs.Remita;

public class RemitaResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class KeyRemitaAuthInfo
{
    public string USERNAME { get; set; } = string.Empty;
    public string channel { get; set; } = string.Empty;
    public string timestamp { get; set; } = string.Empty;
    public string API_KEY { get; set; } = string.Empty;
    public string authtoken { get; set; } = string.Empty;
    public string SecondFa { get; set; } = string.Empty;
    public string SecondFaType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public bool Enforce2FA { get; set; }
}