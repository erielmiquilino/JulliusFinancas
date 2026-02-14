using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Jullius.ServiceApi.Application.Services;

public class BotConfigurationService
{
    private readonly IBotConfigurationRepository _repository;
    private readonly IDataProtector _protector;
    private readonly ILogger<BotConfigurationService> _logger;

    private const string ProtectorPurpose = "Jullius.BotConfiguration.Encryption";

    public BotConfigurationService(
        IBotConfigurationRepository repository,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<BotConfigurationService> logger)
    {
        _repository = repository;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _logger = logger;
    }

    public async Task<IEnumerable<BotConfigurationDto>> GetAllConfigsAsync()
    {
        var configs = await _repository.GetAllAsync();
        return configs.Select(MapToDto);
    }

    public async Task<string?> GetDecryptedValueAsync(string configKey)
    {
        var config = await _repository.GetByKeyAsync(configKey);
        if (config == null)
            return null;

        try
        {
            return _protector.Unprotect(config.EncryptedValue);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex,
                "Não foi possível descriptografar a configuração {ConfigKey}. A chave de criptografia antiga pode ter sido perdida. Regrave este valor para gerar nova criptografia.",
                configKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao descriptografar configuração: {ConfigKey}", configKey);
            return null;
        }
    }

    public async Task<BotConfigurationDto> UpsertConfigAsync(string configKey, UpdateBotConfigurationRequest request)
    {
        var encryptedValue = _protector.Protect(request.Value);
        var existing = await _repository.GetByKeyAsync(configKey);

        if (existing != null)
        {
            existing.UpdateValue(encryptedValue, request.Description);
            await _repository.UpdateAsync(existing);
            _logger.LogInformation("Configuração atualizada: {ConfigKey}", configKey);
            return MapToDto(existing);
        }

        var config = new BotConfiguration(configKey, encryptedValue, request.Description);
        await _repository.CreateAsync(config);
        _logger.LogInformation("Configuração criada: {ConfigKey}", configKey);
        return MapToDto(config);
    }

    public async Task<bool> DeleteConfigAsync(string configKey)
    {
        var existing = await _repository.GetByKeyAsync(configKey);
        if (existing == null)
            return false;

        await _repository.DeleteAsync(configKey);
        _logger.LogInformation("Configuração removida: {ConfigKey}", configKey);
        return true;
    }

    public async Task<bool> HasConfigAsync(string configKey)
    {
        var config = await _repository.GetByKeyAsync(configKey);
        return config != null;
    }

    private static BotConfigurationDto MapToDto(BotConfiguration config) => new()
    {
        ConfigKey = config.ConfigKey,
        Description = config.Description,
        HasValue = true,
        UpdatedAt = config.UpdatedAt
    };
}
