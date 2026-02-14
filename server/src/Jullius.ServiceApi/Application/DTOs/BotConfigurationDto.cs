namespace Jullius.ServiceApi.Application.DTOs;

public class BotConfigurationDto
{
    public string ConfigKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool HasValue { get; set; }
    public DateTime UpdatedAt { get; set; }
}
