using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        TokenService tokenService,
        EmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Tentativa de login inválida para {Email}", request.Email);
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Tentativa de login em conta desativada: {Email}", request.Email);
            throw new UnauthorizedAccessException("Conta desativada. Entre em contato com o administrador.");
        }

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        await _refreshTokenRepository.CreateAsync(refreshToken);

        _logger.LogInformation("Login realizado com sucesso para {Email}", user.Email);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Tentativa de refresh com token inválido ou expirado");
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        var user = storedToken.User;

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Conta desativada.");
        }

        // Revogar o token atual (rotação de refresh token)
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);
        storedToken.Revoke(newRefreshToken.Token);
        await _refreshTokenRepository.UpdateAsync(storedToken);
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);

        _logger.LogInformation("Token renovado com sucesso para {UserId}", user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(storedToken);
            _logger.LogInformation("Logout realizado para UserId {UserId}", storedToken.UserId);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Sempre retorna sucesso para não expor quais e-mails existem
        if (user == null || !user.IsActive)
        {
            _logger.LogInformation("Solicitação de reset para e-mail inexistente/inativo: {Email}", request.Email);
            return;
        }

        // Invalidar tokens anteriores
        await _passwordResetTokenRepository.InvalidateAllByUserIdAsync(user.Id);

        // Gerar novo token
        var tokenValue = _tokenService.GeneratePasswordResetToken();
        var passwordResetToken = new PasswordResetToken(
            tokenValue,
            user.Id,
            DateTime.UtcNow.AddHours(1)
        );
        await _passwordResetTokenRepository.CreateAsync(passwordResetToken);

        // Enviar e-mail
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, tokenValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de reset para {Email}", user.Email);
            // Não propaga exceção para o cliente — token já foi criado
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var storedToken = await _passwordResetTokenRepository.GetByTokenAsync(request.Token);

        if (storedToken == null || !storedToken.IsValid)
        {
            throw new InvalidOperationException("Token de redefinição inválido ou expirado.");
        }

        var user = storedToken.User;

        // Atualizar senha
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatePassword(passwordHash);
        await _userRepository.UpdateAsync(user);

        // Marcar token como usado
        storedToken.MarkAsUsed();
        await _passwordResetTokenRepository.UpdateAsync(storedToken);

        // Revogar todos os refresh tokens do usuário (forçar re-login)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id);

        _logger.LogInformation("Senha redefinida com sucesso para {UserId}", user.Id);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        var user = new User(request.Email, passwordHash, request.Name);
        var created = await _userRepository.CreateAsync(user);

        _logger.LogInformation("Usuário criado: {Email}", created.Email);

        return MapToDto(created);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Senha atual incorreta.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatePassword(passwordHash);
        await _userRepository.UpdateAsync(user);

        // Revogar todos os refresh tokens (forçar re-login em outros dispositivos)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);

        _logger.LogInformation("Senha alterada para UserId {UserId}", userId);
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
