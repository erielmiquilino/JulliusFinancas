using Jullius.ServiceApi.Application.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Jullius.ServiceApi.Telegram;

public class TelegramBotService(
    BotConfigurationService configService,
    ConversationOrchestrator orchestrator,
    ILogger<TelegramBotService> logger)
{
    public async Task<bool> ProcessUpdateAsync(Update update)
    {
        if (update.Message?.Text is not { Length: > 0 } messageText)
            return false;

        var chatId = update.Message.Chat.Id;

        if (!await IsAuthorizedAsync(chatId))
        {
            logger.LogWarning("Mensagem recebida de chat não autorizado: {ChatId}", chatId);
            return false;
        }

        logger.LogInformation("Mensagem recebida de {ChatId}: {Texto}", chatId, messageText[..Math.Min(50, messageText.Length)]);

        var response = await orchestrator.ProcessMessageAsync(chatId, messageText);
        await SendMessageAsync(chatId, response);

        return true;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        var botToken = await configService.GetDecryptedValueAsync("TelegramBotToken");
        if (string.IsNullOrEmpty(botToken))
        {
            logger.LogError("Token do Telegram Bot não configurado");
            return;
        }

        var client = new TelegramBotClient(botToken);

        try
        {
            await client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Markdown);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar mensagem para chat {ChatId}", chatId);

            // Retry without markdown if parsing fails
            try
            {
                await client.SendMessage(
                    chatId: chatId,
                    text: text);
            }
            catch (Exception retryEx)
            {
                logger.LogError(retryEx, "Erro ao reenviar mensagem sem markdown para chat {ChatId}", chatId);
            }
        }
    }

    private async Task<bool> IsAuthorizedAsync(long chatId)
    {
        var authorizedChatId = await configService.GetDecryptedValueAsync("TelegramAuthorizedChatId");
        if (string.IsNullOrEmpty(authorizedChatId))
        {
            logger.LogWarning("TelegramAuthorizedChatId não configurado — permitindo todas as mensagens");
            return true;
        }

        return long.TryParse(authorizedChatId, out var authorized) && authorized == chatId;
    }
}
