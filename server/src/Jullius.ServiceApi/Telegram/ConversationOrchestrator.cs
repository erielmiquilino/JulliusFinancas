using System.Text;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram.IntentHandlers;

namespace Jullius.ServiceApi.Telegram;

public class ConversationOrchestrator(
    ConversationStateStore stateStore,
    GeminiAssistantService geminiService,
    IEnumerable<IIntentHandler> intentHandlers,
    ICategoryRepository categoryRepository,
    ILogger<ConversationOrchestrator> logger)
{
    private static readonly HashSet<string> ConfirmationYes = ["sim", "s", "confirma", "confirmo", "ok", "isso", "pode", "positivo", "yes", "y", "👍"];
    private static readonly HashSet<string> ConfirmationNo = ["não", "nao", "n", "cancela", "cancelar", "desistir", "no", "👎"];
    private static readonly HashSet<string> CancelCommands = ["/cancelar", "/cancel", "/reset"];

    public async Task<string> ProcessMessageAsync(long chatId, string message)
    {
        var state = stateStore.GetOrCreate(chatId);
        var normalizedMessage = message.Trim().ToLowerInvariant();

        try
        {
            var response = state.Phase switch
            {
                ConversationPhase.Idle => await HandleIdlePhaseAsync(state, message, normalizedMessage),
                ConversationPhase.CollectingData => await HandleCollectingPhaseAsync(state, message, normalizedMessage),
                ConversationPhase.AwaitingConfirmation => await HandleConfirmationPhaseAsync(state, normalizedMessage),
                _ => "❌ Estado inesperado. Use /cancelar para recomeçar."
            };

            state.History.Add(new ChatMessage { Role = "user", Content = message });
            state.History.Add(new ChatMessage { Role = "assistant", Content = response });
            state.Touch();

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem do chat {ChatId}", chatId);
            state.Reset();
            return "❌ Ocorreu um erro inesperado. Tente novamente.";
        }
    }

    public async Task<string> ProcessMediaMessageAsync(long chatId, byte[] mediaBytes, string mimeType, string? caption)
    {
        var state = stateStore.GetOrCreate(chatId);

        try
        {
            var intentResponses = await geminiService.ClassifyIntentFromMediaAsync(mediaBytes, mimeType, caption, state.History);
            if (intentResponses is not { Count: > 0 })
            {
                var mediaType = mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ? "áudio" : "imagem";
                return $"❌ Não consegui extrair informações da {mediaType}. Tente enviar novamente ou descreva a transação por texto.";
            }

            // Reuse the same flow as text — populate pending transactions and advance
            state.PendingTransactions.Clear();
            foreach (var resp in intentResponses)
            {
                var intentType = MapIntent(resp.Intent);
                if (intentType == IntentType.Unknown)
                    continue;

                var pending = new PendingTransaction { Intent = intentType };
                PopulatePendingFromExtraction(pending, resp.Data);
                state.PendingTransactions.Add(pending);
            }

            if (state.PendingTransactions.Count == 0)
                return "🤔 Não consegui identificar transações na mídia enviada. Tente descrever por texto.";

            var response = await TryAdvanceToNextIncompleteAsync(state)
                ?? BuildBatchConfirmationMessage(state);

            var mediaDescription = mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ? "🎤 Áudio processado!" : "🖼️ Imagem processada!";
            state.History.Add(new ChatMessage { Role = "user", Content = $"[{mediaDescription}] {caption ?? ""}" });
            state.History.Add(new ChatMessage { Role = "assistant", Content = response });
            state.Touch();

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mídia do chat {ChatId}", chatId);
            state.Reset();
            return "❌ Ocorreu um erro ao processar a mídia. Tente novamente.";
        }
    }

    private async Task<string> HandleIdlePhaseAsync(ConversationState state, string message, string normalizedMessage)
    {
        if (normalizedMessage is "/start" or "/ajuda" or "/help")
            return BuildHelpMessage();

        if (CancelCommands.Contains(normalizedMessage))
            return "✅ Nada a cancelar. Estou pronto para ajudar!";

        var intentResponses = await geminiService.ClassifyIntentAsync(message, state.History);
        if (intentResponses is not { Count: > 0 })
            return "❌ Não consegui entender sua mensagem. Tente reformular.";

        // ── Caso consulta financeira (sem fluxo de confirmação) ──
        if (intentResponses.Count == 1 && MapIntent(intentResponses[0].Intent) == IntentType.FinancialConsulting)
        {
            var resp = intentResponses[0];
            state.CurrentIntent = IntentType.FinancialConsulting;
            state.SetData("question", resp.Data.Question ?? message);
            var handler = GetHandler(IntentType.FinancialConsulting);
            var result = await handler.HandleAsync(state);
            state.Reset();
            return result;
        }

        // ── Montar PendingTransactions a partir das respostas do Gemini ──
        state.PendingTransactions.Clear();
        foreach (var resp in intentResponses)
        {
            var intentType = MapIntent(resp.Intent);
            if (intentType == IntentType.Unknown)
                continue;

            var pending = new PendingTransaction { Intent = intentType };
            PopulatePendingFromExtraction(pending, resp.Data);
            state.PendingTransactions.Add(pending);
        }

        if (state.PendingTransactions.Count == 0)
            return "🤔 Não entendi. Você pode:\n• Registrar um gasto\n• Registrar compra no cartão\n• Fazer uma consulta financeira";

        // ── Verificar dados faltantes em cada transação ──
        return await TryAdvanceToNextIncompleteAsync(state)
            ?? BuildBatchConfirmationMessage(state);
    }

    private async Task<string> HandleCollectingPhaseAsync(ConversationState state, string message, string normalizedMessage)
    {
        if (CancelCommands.Contains(normalizedMessage))
        {
            state.Reset();
            return "❌ Operação cancelada.";
        }

        var currentPending = state.PendingTransactions[state.CurrentTransactionIndex];

        // Use Gemini para extrair dados do follow-up
        var contextHint = currentPending.Intent switch
        {
            IntentType.CreateExpense => "Registrando despesa. Dados já coletados: " + FormatPendingData(currentPending),
            IntentType.CreateCardPurchase => "Registrando compra no cartão. Dados já coletados: " + FormatPendingData(currentPending),
            _ => "Coletando informações"
        };

        var extraction = await geminiService.ExtractDataFromFollowUpAsync(message, contextHint);
        if (extraction?.Data != null)
            MergePendingFromExtraction(currentPending, extraction.Data);

        // Verificar se a transação atual ficou completa
        state.LoadFromPending(currentPending);
        var handler = GetHandler(currentPending.Intent);
        var missingFields = handler.GetMissingFields(state);
        state.SaveToPending(state.CurrentTransactionIndex);

        if (missingFields.Count > 0)
            return await BuildMissingFieldsQuestionAsync(missingFields, extraction?.ClarificationQuestion);

        // Transação atual completa — avançar para a próxima incompleta ou confirmar
        return await TryAdvanceToNextIncompleteAsync(state, state.CurrentTransactionIndex + 1)
            ?? BuildBatchConfirmationMessage(state);
    }

    private async Task<string> HandleConfirmationPhaseAsync(ConversationState state, string normalizedMessage)
    {
        if (CancelCommands.Contains(normalizedMessage))
        {
            state.Reset();
            return "❌ Operação cancelada.";
        }

        if (ConfirmationNo.Contains(normalizedMessage))
        {
            state.Reset();
            return "❌ Operação cancelada. O que deseja fazer?";
        }

        if (ConfirmationYes.Contains(normalizedMessage))
        {
            var results = new List<string>();

            foreach (var pending in state.PendingTransactions)
            {
                state.CurrentIntent = pending.Intent;
                state.CollectedData = new Dictionary<string, object?>(pending.Data);

                var handler = GetHandler(pending.Intent);
                var result = await handler.HandleConfirmationAsync(state, true);
                results.Add(result);
            }

            state.Reset();
            return string.Join("\n\n", results);
        }

        return "Por favor, responda **sim** para confirmar ou **não** para cancelar.";
    }

    // ──────────────── Helpers ────────────────

    /// <summary>
    /// Procura a próxima transação com dados faltantes. Se encontrar, coloca o state em CollectingData.
    /// Retorna null se todas estão completas (prontas para confirmação).
    /// </summary>
    private async Task<string?> TryAdvanceToNextIncompleteAsync(ConversationState state, int startIndex = 0)
    {
        for (var i = startIndex; i < state.PendingTransactions.Count; i++)
        {
            var pending = state.PendingTransactions[i];
            state.LoadFromPending(pending);
            var handler = GetHandler(pending.Intent);
            var missing = handler.GetMissingFields(state);

            if (missing.Count > 0)
            {
                state.CurrentTransactionIndex = i;
                state.Phase = ConversationPhase.CollectingData;

                var prefix = state.IsBatchMode
                    ? $"📌 Transação {i + 1} de {state.PendingTransactions.Count}:\n"
                    : "";

                return prefix + await BuildMissingFieldsQuestionAsync(missing, null);
            }
        }

        // Tudo completo → confirmação
        state.Phase = ConversationPhase.AwaitingConfirmation;
        return null; // sinaliza que deve mostrar confirmação
    }

    private string BuildBatchConfirmationMessage(ConversationState state)
    {
        state.Phase = ConversationPhase.AwaitingConfirmation;

        if (state.PendingTransactions.Count == 1)
        {
            var pending = state.PendingTransactions[0];
            state.LoadFromPending(pending);
            var handler = GetHandler(pending.Intent);
            return handler.BuildConfirmationMessage(state);
        }

        // Batch: construir mensagem combinada
        var sb = new StringBuilder();
        sb.AppendLine($"📝 *Confirma {state.PendingTransactions.Count} lançamentos?*\n");

        for (var i = 0; i < state.PendingTransactions.Count; i++)
        {
            var tx = state.PendingTransactions[i];
            var emoji = tx.Intent == IntentType.CreateCardPurchase ? "💳" : "💸";
            var desc = tx.GetData<string>("description") ?? "N/A";
            var amount = tx.GetData<decimal>("amount");
            var category = tx.GetData<string>("categoryName");
            var card = tx.GetData<string>("cardName");
            var isPaid = tx.GetData<bool>("isPaid");
            var paidText = isPaid ? " ✅ Pago" : "";
            var target = category ?? card ?? "";

            sb.AppendLine($"{i + 1}. {emoji} {desc} — R$ {amount:N2} em {target}{paidText}");
        }

        sb.AppendLine("\nResponda *sim* para confirmar ou *não* para cancelar.");
        return sb.ToString();
    }

    private IIntentHandler GetHandler(IntentType intentType)
    {
        return intentHandlers.First(h => h.HandledIntent == intentType);
    }

    private static IntentType MapIntent(string intentString)
    {
        return intentString?.ToUpperInvariant() switch
        {
            "CREATE_EXPENSE" => IntentType.CreateExpense,
            "CREATE_CARD_PURCHASE" => IntentType.CreateCardPurchase,
            "FINANCIAL_CONSULTING" => IntentType.FinancialConsulting,
            _ => IntentType.Unknown
        };
    }

    private static void PopulatePendingFromExtraction(PendingTransaction pending, GeminiExtractedData data)
    {
        if (!string.IsNullOrEmpty(data.Description)) pending.SetData("description", data.Description);
        if (data.Amount.HasValue) pending.SetData("amount", data.Amount.Value);
        if (!string.IsNullOrEmpty(data.CategoryName)) pending.SetData("categoryName", data.CategoryName);
        if (!string.IsNullOrEmpty(data.CardName)) pending.SetData("cardName", data.CardName);
        if (data.Installments.HasValue) pending.SetData("installments", data.Installments.Value);
        if (data.IsPaid.HasValue) pending.SetData("isPaid", data.IsPaid.Value);
        if (data.DueDate.HasValue) pending.SetData("dueDate", EnsureUtc(data.DueDate.Value));
        if (!string.IsNullOrEmpty(data.Question)) pending.SetData("question", data.Question);
    }

    private static void MergePendingFromExtraction(PendingTransaction pending, GeminiExtractedData data)
    {
        if (!string.IsNullOrEmpty(data.Description) && !pending.HasData("description")) pending.SetData("description", data.Description);
        if (data.Amount.HasValue && !pending.HasData("amount")) pending.SetData("amount", data.Amount.Value);
        if (!string.IsNullOrEmpty(data.CategoryName) && !pending.HasData("categoryName")) pending.SetData("categoryName", data.CategoryName);
        if (!string.IsNullOrEmpty(data.CardName) && !pending.HasData("cardName")) pending.SetData("cardName", data.CardName);
        if (data.Installments.HasValue && !pending.HasData("installments")) pending.SetData("installments", data.Installments.Value);
        if (data.IsPaid.HasValue && !pending.HasData("isPaid")) pending.SetData("isPaid", data.IsPaid.Value);
        if (data.DueDate.HasValue && !pending.HasData("dueDate")) pending.SetData("dueDate", EnsureUtc(data.DueDate.Value));
    }

    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

    private static string FormatPendingData(PendingTransaction pending)
    {
        var parts = new List<string>();
        if (pending.HasData("description")) parts.Add($"descrição='{pending.GetData<string>("description")}'");
        if (pending.HasData("amount")) parts.Add($"valor={pending.GetData<decimal>("amount")}");
        if (pending.HasData("categoryName")) parts.Add($"categoria='{pending.GetData<string>("categoryName")}'");
        if (pending.HasData("cardName")) parts.Add($"cartão='{pending.GetData<string>("cardName")}'");
        if (pending.HasData("installments")) parts.Add($"parcelas={pending.GetData<int>("installments")}");
        if (pending.HasData("isPaid")) parts.Add($"pago={pending.GetData<bool>("isPaid")}");
        if (pending.HasData("dueDate")) parts.Add($"vencimento={pending.GetData<DateTime>("dueDate"):dd/MM/yyyy}");
        return string.Join(", ", parts);
    }

    private async Task<string> BuildMissingFieldsQuestionAsync(List<string> missingFields, string? clarificationQuestion)
    {
        if (!string.IsNullOrEmpty(clarificationQuestion))
            return clarificationQuestion;

        var fieldNames = new List<string>();
        foreach (var f in missingFields)
        {
            if (f == "categoryName")
            {
                var categories = await categoryRepository.GetAllAsync();
                var categoryList = categories.ToList();
                if (categoryList.Count > 0)
                {
                    var names = string.Join(", ", categoryList.Select(c => c.Name));
                    fieldNames.Add($"🏷️ Categoria — Suas categorias: {names}");
                }
                else
                {
                    fieldNames.Add("🏷️ Categoria (ex: Alimentação)");
                }
            }
            else
            {
                fieldNames.Add(f switch
                {
                    "description" => "📝 Descrição (ex: Almoço no restaurante)",
                    "amount" => "💰 Valor (ex: 45.90)",
                    "cardName" => "💳 Cartão (ex: Nubank)",
                    _ => f
                });
            }
        }

        return "Preciso das seguintes informações:\n" + string.Join("\n", fieldNames);
    }

    private static string BuildHelpMessage()
    {
        return """
            🤖 **Jullius Finanças — Assistente Telegram**

            Posso te ajudar com:

            💸 **Registrar despesa**
            "Gastei 45 reais de almoço"
            "Paguei 120 de internet"

            💳 **Registrar compra no cartão**
            "Comprei no Nubank 500 reais em 3x"
            "Parcelei 2000 no Inter em 10 vezes"

            📊 **Consulta financeira**
            "Como estou esse mês?"
            "Quanto gastei com alimentação?"
            "Posso gastar 500 reais?"

            📦 **Múltiplas transações**
            "Gastei 50 de almoço e 30 de café, as duas pagas"
            "Lance 100 em saúde e 200 em transporte"

            🖼️ **Enviar imagem**
            Envie uma foto de comprovante ou notificação para registrar automaticamente.

            🎤 **Enviar áudio**
            Grave um áudio descrevendo seus gastos e eu transcrevo e registro.

            📌 **Comandos:**
            /start — Esta mensagem
            /cancelar — Cancelar operação em andamento
            """;
    }
}
