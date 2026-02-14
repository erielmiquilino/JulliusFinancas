using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;

namespace Jullius.ServiceApi.Telegram.IntentHandlers;

public class CreateCardPurchaseHandler : IIntentHandler
{
    private readonly CardTransactionService _cardTransactionService;
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<CreateCardPurchaseHandler> _logger;

    public IntentType HandledIntent => IntentType.CreateCardPurchase;

    public CreateCardPurchaseHandler(
        CardTransactionService cardTransactionService,
        ICardRepository cardRepository,
        ILogger<CreateCardPurchaseHandler> logger)
    {
        _cardTransactionService = cardTransactionService;
        _cardRepository = cardRepository;
        _logger = logger;
    }

    public List<string> GetMissingFields(ConversationState state)
    {
        var missing = new List<string>();
        if (!state.HasData("description")) missing.Add("description");
        if (!state.HasData("amount")) missing.Add("amount");
        if (!state.HasData("cardName")) missing.Add("cardName");
        return missing;
    }

    public string BuildConfirmationMessage(ConversationState state)
    {
        var description = state.GetData<string>("description") ?? "N/A";
        var amount = state.GetData<decimal>("amount");
        var cardName = state.GetData<string>("cardName") ?? "N/A";
        var installments = state.GetData<int?>("installments") ?? 1;
        var installmentAmount = installments > 1 ? amount / installments : amount;

        var installmentText = installments > 1
            ? $"{installments}x de R$ {installmentAmount:N2}"
            : "À vista";

        return $"""
            💳 *Confirma a compra no cartão?*

            • Descrição: {description}
            • Valor total: R$ {amount:N2}
            • Parcelas: {installmentText}
            • Cartão: {cardName}

            Responda *sim* para confirmar ou *não* para cancelar.
            """;
    }

    public async Task<string> HandleAsync(ConversationState state)
    {
        var missing = GetMissingFields(state);
        if (missing.Count > 0)
        {
            state.Phase = ConversationPhase.CollectingData;
            return await BuildMissingFieldQuestion(missing.First(), state);
        }

        // Resolve card by name before confirmation
        var cardName = state.GetData<string>("cardName")!;
        var card = await FindCardByNameAsync(cardName);

        if (card == null)
        {
            var allCards = await _cardRepository.GetAllAsync();
            var cardList = allCards.ToList();

            if (cardList.Count == 0)
                return "❌ Nenhum cartão cadastrado. Cadastre um cartão primeiro pelo app.";

            var cardNames = string.Join("\n", cardList.Select(c => $"• {c.Name} ({c.IssuingBank})"));
            return $"💳 Não encontrei um cartão com esse nome. Seus cartões são:\n{cardNames}\n\nQual deseja usar?";
        }

        state.SetData("cardId", card.Id);
        state.SetData("cardName", card.Name);
        state.Phase = ConversationPhase.AwaitingConfirmation;
        return BuildConfirmationMessage(state);
    }

    public async Task<string> HandleConfirmationAsync(ConversationState state, bool confirmed)
    {
        if (!confirmed)
            return "❌ Compra cancelada.";

        try
        {
            var cardId = state.GetData<Guid>("cardId");
            var card = await _cardRepository.GetByIdAsync(cardId);
            if (card == null)
                return "❌ Cartão não encontrado. Tente novamente.";

            var installments = state.GetData<int?>("installments") ?? 1;
            var amount = state.GetData<decimal>("amount");
            var now = DateTime.UtcNow;

            // Calculate invoice period based on card's closing/due day
            var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(now, card.ClosingDay, card.DueDay);

            var request = new CreateCardTransactionRequest
            {
                CardId = cardId,
                Description = state.GetData<string>("description")!,
                Amount = amount,
                Date = now,
                IsInstallment = installments > 1,
                InstallmentCount = installments,
                Type = CardTransactionType.Expense,
                InvoiceYear = invoiceYear,
                InvoiceMonth = invoiceMonth
            };

            var transactions = await _cardTransactionService.CreateCardTransactionAsync(request);

            // Reload card to get updated limit
            card = await _cardRepository.GetByIdAsync(cardId);
            var installmentText = installments > 1
                ? $"{installments}x R$ {Math.Round(amount / installments, 2):N2}"
                : $"R$ {amount:N2}";

            return $"""
                ✅ Compra registrada com sucesso!
                • {request.Description} — {installmentText} no {card!.Name}
                • Limite restante: R$ {card.CurrentLimit:N2}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar transação de cartão via Telegram");
            return $"❌ Erro ao registrar a compra: {ex.Message}";
        }
    }

    private async Task<Card?> FindCardByNameAsync(string name)
    {
        var allCards = await _cardRepository.GetAllAsync();
        var normalizedName = name.Trim().ToLowerInvariant();

        return allCards.FirstOrDefault(c =>
            c.Name.Trim().ToLowerInvariant().Contains(normalizedName) ||
            normalizedName.Contains(c.Name.Trim().ToLowerInvariant()) ||
            c.IssuingBank.Trim().ToLowerInvariant().Contains(normalizedName));
    }

    private static (int Year, int Month) CalculateInvoicePeriod(DateTime transactionDate, int closingDay, int dueDay)
    {
        DateTime effectiveClosingDate;

        if (transactionDate.Day > closingDay)
            effectiveClosingDate = new DateTime(transactionDate.Year, transactionDate.Month, closingDay).AddMonths(1);
        else
            effectiveClosingDate = new DateTime(transactionDate.Year, transactionDate.Month, closingDay);

        DateTime invoiceDueDate;
        if (dueDay <= closingDay)
        {
            var monthOfDueDate = effectiveClosingDate.AddMonths(1);
            invoiceDueDate = new DateTime(monthOfDueDate.Year, monthOfDueDate.Month, dueDay);
        }
        else
        {
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay);
        }

        return (invoiceDueDate.Year, invoiceDueDate.Month);
    }

    private async Task<string> BuildMissingFieldQuestion(string field, ConversationState state)
    {
        if (field == "cardName")
        {
            var allCards = await _cardRepository.GetAllAsync();
            var cardList = allCards.ToList();
            if (cardList.Count > 0)
            {
                var names = string.Join(", ", cardList.Select(c => c.Name));
                return $"💳 Em qual cartão? Seus cartões: {names}";
            }
            return "💳 Em qual cartão?";
        }

        return field switch
        {
            "description" => "📝 Qual a descrição da compra?",
            "amount" => "💰 Qual o valor total?",
            _ => $"❓ Informe o campo: {field}"
        };
    }
}
