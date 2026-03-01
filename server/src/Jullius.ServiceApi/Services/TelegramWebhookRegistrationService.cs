using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;

namespace Jullius.ServiceApi.Services;

/// <summary>
/// Serviço hospedado que registra o webhook do Telegram automaticamente na inicialização.
/// A URL base é lida de Telegram:WebhookBaseUrl no appsettings (varia por ambiente).
/// </summary>
public class TelegramWebhookRegistrationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TelegramWebhookRegistrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = configuration["Telegram:WebhookBaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning(
                "Telegram:WebhookBaseUrl não configurada. O webhook não será registrado automaticamente.");
            return;
        }

        // Aguarda brevemente para que o banco e migrations estejam prontos
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            await RegisterWebhookAsync(baseUrl, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Aplicação encerrando, ignorar
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao registrar webhook do Telegram na inicialização. " +
                "Verifique se o TelegramBotToken está configurado no banco.");
        }
    }

    private async Task RegisterWebhookAsync(string baseUrl, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<BotConfigurationService>();

        var token = await configService.GetDecryptedValueAsync("TelegramBotToken");
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning(
                "TelegramBotToken não configurado no banco. Webhook não será registrado. " +
                "Configure o token na tela de Configurações e reinicie a aplicação.");
            return;
        }

        // Garante que o webhook secret existe
        var webhookSecret = await configService.GetDecryptedValueAsync("TelegramWebhookSecret");
        if (string.IsNullOrEmpty(webhookSecret))
        {
            webhookSecret = Guid.NewGuid().ToString("N");
            await configService.UpsertConfigAsync("TelegramWebhookSecret", new UpdateBotConfigurationRequest
            {
                Value = webhookSecret,
                Description = "Token secreto do webhook (gerado automaticamente)"
            });
            logger.LogInformation("TelegramWebhookSecret gerado automaticamente");
        }

        var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/telegram/webhook/{webhookSecret}";

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(
            $"https://api.telegram.org/bot{token}/setWebhook?url={Uri.EscapeDataString(webhookUrl)}",
            ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "Webhook do Telegram registrado com sucesso na inicialização: {WebhookUrl}",
                webhookUrl);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Falha ao registrar webhook do Telegram. Status: {Status}, Erro: {Erro}",
                response.StatusCode, error);
        }
    }
}
