using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController(
    TelegramBotService botService,
    BotConfigurationService configService,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook/{secret}")]
    public async Task<IActionResult> Webhook([FromRoute] string secret, [FromBody] Update update)
    {
        var expectedSecret = await configService.GetDecryptedValueAsync("TelegramWebhookSecret");
        if (string.IsNullOrEmpty(expectedSecret) || secret != expectedSecret)
        {
            logger.LogWarning("Requisição webhook com secret inválido");
            return Unauthorized();
        }

        await botService.ProcessUpdateAsync(update);
        return Ok();
    }
}
