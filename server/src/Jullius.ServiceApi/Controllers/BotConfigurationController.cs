using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BotConfigurationController : ControllerBase
{
    private readonly BotConfigurationService _service;
    private readonly ILogger<BotConfigurationController> _logger;

    public BotConfigurationController(BotConfigurationService service, ILogger<BotConfigurationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Buscando todas as configurações do bot");
        var configs = await _service.GetAllConfigsAsync();
        return Ok(configs);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        _logger.LogInformation("Buscando configuração: {ConfigKey}", key);
        var value = await _service.GetDecryptedValueAsync(key);
        if (value == null)
            return NotFound();

        return Ok(new { configKey = key, value });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(string key, [FromBody] UpdateBotConfigurationRequest request)
    {
        _logger.LogInformation("Atualizando configuração: {ConfigKey}", key);

        try
        {
            var result = await _service.UpsertConfigAsync(key, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao atualizar configuração {ConfigKey}: {Erro}", key, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        _logger.LogInformation("Removendo configuração: {ConfigKey}", key);
        var deleted = await _service.DeleteConfigAsync(key);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("test-telegram")]
    public async Task<IActionResult> TestTelegramConnection()
    {
        _logger.LogInformation("Testando conexão com Telegram Bot");

        var token = await _service.GetDecryptedValueAsync("TelegramBotToken");
        if (string.IsNullOrEmpty(token))
            return BadRequest("Token do Telegram não configurado");

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://api.telegram.org/bot{token}/getMe");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Conexão com Telegram Bot bem sucedida");
                return Ok(new { success = true, message = "Conexão com o Telegram Bot estabelecida com sucesso", details = content });
            }

            _logger.LogWarning("Falha na conexão com Telegram Bot. Status: {Status}", response.StatusCode);
            return BadRequest(new { success = false, message = "Token do Telegram inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar conexão com Telegram");
            return BadRequest(new { success = false, message = $"Erro ao conectar: {ex.Message}" });
        }
    }

    [HttpPost("test-gemini")]
    public async Task<IActionResult> TestGeminiConnection()
    {
        _logger.LogInformation("Testando conexão com Gemini API");

        var apiKey = await _service.GetDecryptedValueAsync("GeminiApiKey");
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest("Chave API do Gemini não configurada");

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Conexão com Gemini API bem sucedida");
                return Ok(new { success = true, message = "Conexão com o Gemini API estabelecida com sucesso" });
            }

            _logger.LogWarning("Falha na conexão com Gemini API. Status: {Status}", response.StatusCode);
            return BadRequest(new { success = false, message = "Chave API do Gemini inválida" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar conexão com Gemini");
            return BadRequest(new { success = false, message = $"Erro ao conectar: {ex.Message}" });
        }
    }

    [HttpPost("register-webhook")]
    public async Task<IActionResult> RegisterWebhook([FromBody] RegisterWebhookRequest request)
    {
        _logger.LogInformation("Registrando webhook do Telegram");

        var token = await _service.GetDecryptedValueAsync("TelegramBotToken");
        if (string.IsNullOrEmpty(token))
            return BadRequest("Token do Telegram não configurado");

        var webhookSecret = await _service.GetDecryptedValueAsync("TelegramWebhookSecret");
        if (string.IsNullOrEmpty(webhookSecret))
        {
            webhookSecret = Guid.NewGuid().ToString("N");
            await _service.UpsertConfigAsync("TelegramWebhookSecret", new UpdateBotConfigurationRequest
            {
                Value = webhookSecret,
                Description = "Token secreto do webhook (gerado automaticamente)"
            });
        }

        var webhookUrl = $"{request.BaseUrl.TrimEnd('/')}/api/telegram/webhook/{webhookSecret}";

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(
                $"https://api.telegram.org/bot{token}/setWebhook?url={Uri.EscapeDataString(webhookUrl)}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook registrado com sucesso: {WebhookUrl}", webhookUrl);
                return Ok(new { success = true, message = "Webhook registrado com sucesso", webhookUrl });
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Falha ao registrar webhook. Erro: {Erro}", error);
            return BadRequest(new { success = false, message = $"Falha ao registrar webhook: {error}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar webhook");
            return BadRequest(new { success = false, message = $"Erro ao registrar webhook: {ex.Message}" });
        }
    }
}

public class RegisterWebhookRequest
{
    public string BaseUrl { get; set; } = string.Empty;
}
