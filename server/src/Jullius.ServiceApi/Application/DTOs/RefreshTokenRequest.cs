using System.ComponentModel.DataAnnotations;

namespace Jullius.ServiceApi.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
