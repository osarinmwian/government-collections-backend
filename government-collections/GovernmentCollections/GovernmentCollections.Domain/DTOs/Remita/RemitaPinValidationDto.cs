using System.Text.Json.Serialization;

namespace GovernmentCollections.Domain.DTOs.Remita;

public class RemitaPinValidationDto
{
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
    [JsonIgnore]
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