using System.ComponentModel;
using System.Globalization;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram.Plugins;

/// <summary>
/// Plugin SK para criação e consulta de transações de cartão de crédito.
/// Substitui CreateCardPurchaseHandler.
/// </summary>
public sealed class CardTransactionPlugin
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly CardTransactionService _cardTransactionService;
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<CardTransactionPlugin> _logger;

    public CardTransactionPlugin(
        CardTransactionService cardTransactionService,
        ICardRepository cardRepository,
        ILogger<CardTransactionPlugin> logger)
    {
        _cardTransactionService = cardTransactionService;
        _cardRepository = cardRepository;
        _logger = logger;
    }

    [KernelFunction("CreateCardPurchase")]
    [Description("Registra uma compra no cartão de crédito. Use quando o usuário menciona cartão, parcelas, ou nome de cartão (nubank, inter, itaú, etc). Retorna confirmação com dados registrados.")]
    public async Task<string> CreateCardPurchaseAsync(
        [Description("Descrição da compra, com primeira letra maiúscula (ex: 'Tênis Nike', 'Jantar restaurante')")] string description,
        [Description("Valor total da compra")] decimal amount,
        [Description("Nome do cartão informado pelo usuário (ex: 'Nubank', 'Inter', 'Itaú'). Será feita busca aproximada.")] string cardName,
        [Description("Número de parcelas (1 para à vista). Interprete '10x', 'em 10 vezes', etc.")] int installments = 1)
    {
        try
        {
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

            var now = DateTime.UtcNow;
            var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(now, card.ClosingDay, card.DueDay);

            var request = new CreateCardTransactionRequest
            {
                CardId = card.Id,
                Description = description,
                Amount = amount,
                Date = now,
                IsInstallment = installments > 1,
                InstallmentCount = installments,
                Type = CardTransactionType.Expense,
                InvoiceYear = invoiceYear,
                InvoiceMonth = invoiceMonth
            };

            await _cardTransactionService.CreateCardTransactionAsync(request);

            // Reload card to get updated limit
            card = await _cardRepository.GetByIdAsync(card.Id);

            var installmentText = installments > 1
                ? $"{installments}x de R$ {(amount / installments).ToString("N2", PtBrCulture)}"
                : $"R$ {amount.ToString("N2", PtBrCulture)} à vista";

            return $"""
                ✅ Compra registrada no cartão!
                • {description} — {installmentText} no {card!.Name}
                • Limite restante: R$ {card.CurrentLimit.ToString("N2", PtBrCulture)}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar transação de cartão via Telegram SK");
            return $"❌ Erro ao registrar a compra no cartão: {ex.Message}";
        }
    }

    [KernelFunction("ListCards")]
    [Description("Lista todos os cartões de crédito cadastrados com nome, banco, limite e dias de fechamento/vencimento.")]
    public async Task<string> ListCardsAsync()
    {
        try
        {
            var allCards = await _cardRepository.GetAllAsync();
            var cards = allCards.ToList();

            if (cards.Count == 0)
                return "💳 Nenhum cartão cadastrado.";

            var lines = cards.Select(c =>
                $"• {c.Name} ({c.IssuingBank}) — Limite: R$ {c.Limit.ToString("N2", PtBrCulture)} | Disponível: R$ {c.CurrentLimit.ToString("N2", PtBrCulture)} | Fecha dia {c.ClosingDay}, vence dia {c.DueDay}");

            return $"💳 Seus cartões:\n{string.Join("\n", lines)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar cartões via Telegram SK");
            return $"❌ Erro ao listar cartões: {ex.Message}";
        }
    }

    [KernelFunction("GetCardInvoice")]
    [Description("Consulta a fatura de um cartão em um mês/ano específico, mostrando todas as transações e o total.")]
    public async Task<string> GetCardInvoiceAsync(
        [Description("Nome do cartão (ex: 'Nubank')")] string cardName,
        [Description("Mês da fatura (1-12)")] int month,
        [Description("Ano da fatura (ex: 2025)")] int year)
    {
        try
        {
            var card = await FindCardByNameAsync(cardName);
            if (card == null)
                return $"❌ Cartão \"{cardName}\" não encontrado. Use ListCards para ver seus cartões.";

            var invoice = await _cardTransactionService.GetCardTransactionsForInvoiceAsync(card.Id, month, year);

            if (!invoice.Transactions.Any())
                return $"💳 Nenhuma transação na fatura de {month:D2}/{year} do {card.Name}.";

            var lines = invoice.Transactions.Select(t =>
                $"• {t.Description} — R$ {t.Amount.ToString("N2", PtBrCulture)} ({t.Installment})");

            return $"""
                💳 Fatura {month:D2}/{year} — {invoice.CardName}
                {string.Join("\n", lines)}

                Total: R$ {invoice.InvoiceTotal.ToString("N2", PtBrCulture)}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar fatura via Telegram SK");
            return $"❌ Erro ao consultar fatura: {ex.Message}";
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

    internal static (int Year, int Month) CalculateInvoicePeriod(DateTime transactionDate, int closingDay, int dueDay)
    {
        DateTime effectiveClosingDate;

        if (transactionDate.Day > closingDay)
            effectiveClosingDate = new DateTime(transactionDate.Year, transactionDate.Month, closingDay, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        else
            effectiveClosingDate = new DateTime(transactionDate.Year, transactionDate.Month, closingDay, 0, 0, 0, DateTimeKind.Utc);

        DateTime invoiceDueDate;
        if (dueDay <= closingDay)
        {
            var monthOfDueDate = effectiveClosingDate.AddMonths(1);
            invoiceDueDate = new DateTime(monthOfDueDate.Year, monthOfDueDate.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);
        }
        else
        {
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);
        }

        return (invoiceDueDate.Year, invoiceDueDate.Month);
    }
}
