using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardService
{
    private readonly ICardRepository _repository;
    private readonly IFinancialTransactionRepository _financialTransactionRepository;
    private readonly ICardTransactionRepository _cardTransactionRepository;

    public CardService(ICardRepository repository, IFinancialTransactionRepository financialTransactionRepository, ICardTransactionRepository cardTransactionRepository)
    {
        _repository = repository;
        _financialTransactionRepository = financialTransactionRepository;
        _cardTransactionRepository = cardTransactionRepository;
    }

    public async Task<Card> CreateCardAsync(CreateCardRequest request)
    {
        var card = new Card(
            request.Name,
            request.IssuingBank,
            request.ClosingDay,
            request.DueDay,
            request.Limit
        );

        return await _repository.CreateAsync(card);
    }

    public async Task<Card?> GetCardByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Card>> GetAllCardsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Card?> UpdateCardAsync(Guid id, UpdateCardRequest request)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card == null)
            return null;

        var oldLimit = card.Limit;

        card.UpdateDetails(
            request.Name,
            request.IssuingBank,
            request.ClosingDay,
            request.DueDay,
            request.Limit
        );

        // Se o limite total foi alterado, recalcula o limite disponível
        if (oldLimit != request.Limit)
        {
            await RecalculateCurrentLimitAsync(card);
        }

        await _repository.UpdateAsync(card);
        return card;
    }

    public async Task<bool> DeleteCardAsync(Guid id)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card == null)
            return false;

        // Exclui todas as faturas relacionadas ao cartão
        await DeleteCardInvoicesAsync(id);

        // Exclui o cartão (as CardTransactions serão excluídas automaticamente por cascade)
        await _repository.DeleteAsync(id);
        return true;
    }

    private async Task DeleteCardInvoicesAsync(Guid cardId)
    {
        // Busca todas as faturas relacionadas ao cartão usando CardId
        var cardInvoices = await _financialTransactionRepository.GetByCardIdAsync(cardId);

        // Exclui cada fatura encontrada
        foreach (var invoice in cardInvoices)
        {
            await _financialTransactionRepository.DeleteAsync(invoice.Id);
        }
    }

    private async Task RecalculateCurrentLimitAsync(Card card)
    {
        // Calcula qual é a fatura atual baseada nos dias de fechamento e vencimento
        var currentInvoice = CalculateCurrentInvoicePeriod(card.ClosingDay, card.DueDay);

        // Busca todas as transações da fatura atual em diante
        var futureTransactions = await _cardTransactionRepository
            .GetByCardIdFromPeriodAsync(card.Id, currentInvoice.Month, currentInvoice.Year);

        // Calcula o total usado (despesas positivas, receitas negativas)
        var totalUsed = futureTransactions.Sum(t => 
            t.Type == CardTransactionType.Expense ? t.Amount : -t.Amount);

        // Recalcula o limite disponível: limite total - valor usado
        var newCurrentLimit = card.Limit - totalUsed;
        
        card.SetCurrentLimit(newCurrentLimit);
    }

    /// <summary>
    /// Calcula o período da fatura atual baseado na data de hoje e nos dias de fechamento/vencimento do cartão.
    /// Replica a lógica do método calculateCurrentInvoicePeriod do frontend.
    /// </summary>
    private (int Year, int Month) CalculateCurrentInvoicePeriod(int closingDay, int dueDay)
    {
        var today = DateTime.Today;

        DateTime effectiveClosingDate;

        if (today.Day > closingDay)
            // Se hoje é depois do dia de fechamento, a data efetiva de fechamento é no próximo mês
            effectiveClosingDate = new DateTime(today.Year, today.Month, closingDay).AddMonths(1);
        else
            // Se hoje é antes ou no dia de fechamento, a data efetiva é neste mês
            effectiveClosingDate = new DateTime(today.Year, today.Month, closingDay);

        DateTime invoiceDueDate;

        if (dueDay <= closingDay)
        {
            // Se o vencimento é antes ou no dia de fechamento, vai para o próximo mês
            var monthOfDueDate = effectiveClosingDate.AddMonths(1);
            invoiceDueDate = new DateTime(monthOfDueDate.Year, monthOfDueDate.Month, dueDay);
        }
        else
            // Se o vencimento é depois do fechamento, fica no mesmo mês do fechamento
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay);

        return (invoiceDueDate.Year, invoiceDueDate.Month);
    }
} 