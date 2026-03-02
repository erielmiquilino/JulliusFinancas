using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using System.Security.Claims;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Tentativa de login para {Email}", request.Email);

        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("Login realizado com sucesso para {Email}", request.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login falhou para {Email}: {Motivo}", request.Email, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("Tentativa de refresh token");

        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            _logger.LogInformation("Refresh token realizado com sucesso");
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Refresh token falhou: {Motivo}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("Tentativa de logout");

        try
        {
            await _authService.LogoutAsync(request.RefreshToken);
            _logger.LogInformation("Logout realizado com sucesso");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Erro no logout: {Motivo}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("Solicitação de recuperação de senha para {Email}", request.Email);

        // Sempre retorna Ok para evitar enumeração de usuários (timing-safe no service)
        await _authService.ForgotPasswordAsync(request);
        
        return Ok(new { message = "Se o e-mail existir em nossa base, um link de recuperação será enviado." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation("Tentativa de reset de senha via token");

        try
        {
            await _authService.ResetPasswordAsync(request);
            _logger.LogInformation("Reset de senha realizado com sucesso");
            return Ok(new { message = "Senha alterada com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Reset de senha falhou: {Motivo}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Criação de novo usuário: {Email}", request.Email);

        try
        {
            var user = await _authService.CreateUserAsync(request);
            _logger.LogInformation("Usuário criado com sucesso: {Email}, Id: {UserId}", user.Email, user.Id);
            return CreatedAtAction(nameof(GetCurrentUser), user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Criação de usuário falhou para {Email}: {Motivo}", request.Email, ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Alteração de senha para usuário {UserId}", userId);

        try
        {
            await _authService.ChangePasswordAsync(userId, request);
            _logger.LogInformation("Senha alterada com sucesso para usuário {UserId}", userId);
            return Ok(new { message = "Senha alterada com sucesso." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Alteração de senha falhou para {UserId}: {Motivo}", userId, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Busca de dados do usuário {UserId}", userId);

        try
        {
            var user = await _authService.GetCurrentUserAsync(userId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Usuário {UserId} não encontrado: {Motivo}", userId, ex.Message);
            return NotFound(new { message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Token inválido: userId não encontrado.");

        return userId;
    }
}
