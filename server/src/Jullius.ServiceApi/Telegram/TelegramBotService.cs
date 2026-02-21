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
        if (update.Message is null)
            return false;

        var chatId = update.Message.Chat.Id;

        if (!await IsAuthorizedAsync(chatId))
        {
            logger.LogWarning("Mensagem recebida de chat não autorizado: {ChatId}", chatId);
            return false;
        }

        // Handle photo messages
        if (update.Message.Photo is { Length: > 0 })
        {
            var photo = update.Message.Photo[^1]; // highest resolution
            var caption = update.Message.Caption;
            logger.LogInformation("Foto recebida de {ChatId}", chatId);

            var fileBytes = await DownloadFileAsync(photo.FileId);
            if (fileBytes == null)
            {
                await SendMessageAsync(chatId, "❌ Não consegui baixar a imagem. Tente novamente.");
                return false;
            }

            var response = await orchestrator.ProcessMediaMessageAsync(chatId, fileBytes, "image/jpeg", caption);
            await SendMessageAsync(chatId, response);
            return true;
        }

        // Handle voice messages
        if (update.Message.Voice is not null)
        {
            var voice = update.Message.Voice;
            logger.LogInformation("Áudio recebido de {ChatId}", chatId);

            var fileBytes = await DownloadFileAsync(voice.FileId);
            if (fileBytes == null)
            {
                await SendMessageAsync(chatId, "❌ Não consegui baixar o áudio. Tente novamente.");
                return false;
            }

            var mimeType = voice.MimeType ?? "audio/ogg";
            var response = await orchestrator.ProcessMediaMessageAsync(chatId, fileBytes, mimeType, null);
            await SendMessageAsync(chatId, response);
            return true;
        }

        // Handle audio messages (audio files sent as documents/music)
        if (update.Message.Audio is not null)
        {
            var audio = update.Message.Audio;
            logger.LogInformation("Arquivo de áudio recebido de {ChatId}", chatId);

            var fileBytes = await DownloadFileAsync(audio.FileId);
            if (fileBytes == null)
            {
                await SendMessageAsync(chatId, "❌ Não consegui baixar o áudio. Tente novamente.");
                return false;
            }

            var mimeType = audio.MimeType ?? "audio/mpeg";
            var response = await orchestrator.ProcessMediaMessageAsync(chatId, fileBytes, mimeType, null);
            await SendMessageAsync(chatId, response);
            return true;
        }

        // Handle text messages
        if (update.Message.Text is not { Length: > 0 } messageText)
            return false;

        logger.LogInformation("Mensagem recebida de {ChatId}: {Texto}", chatId, messageText[..Math.Min(50, messageText.Length)]);

        var textResponse = await orchestrator.ProcessMessageAsync(chatId, messageText);
        await SendMessageAsync(chatId, textResponse);

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

    private async Task<byte[]?> DownloadFileAsync(string fileId)
    {
        var botToken = await configService.GetDecryptedValueAsync("TelegramBotToken");
        if (string.IsNullOrEmpty(botToken))
            return null;

        try
        {
            var client = new TelegramBotClient(botToken);
            var file = await client.GetFile(fileId);

            if (string.IsNullOrEmpty(file.FilePath))
                return null;

            using var stream = new MemoryStream();
            await client.DownloadFile(file.FilePath, stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao baixar arquivo do Telegram. FileId: {FileId}", fileId);
            return null;
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
