namespace Jullius.ServiceApi.Application.DTOs;

public class UpdateBotConfigurationRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
