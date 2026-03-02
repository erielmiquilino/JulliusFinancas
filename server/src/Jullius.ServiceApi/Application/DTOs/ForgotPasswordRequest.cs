using System.ComponentModel.DataAnnotations;

namespace Jullius.ServiceApi.Application.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
