using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardTransactionService
{
    private readonly ICardTransactionRepository _repository;

    public CardTransactionService(ICardTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CardTransaction> CreateCardTransactionAsync(CreateCardTransactionRequest request)
    {
        var cardTransaction = new CardTransaction(
            request.CardId,
            request.Description,
            request.Amount,
            request.Date,
            request.Installment
        );

        return await _repository.CreateAsync(cardTransaction);
    }

    public async Task<CardTransaction?> GetCardTransactionByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CardTransaction>> GetAllCardTransactionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<CardTransaction>> GetCardTransactionsByCardIdAsync(Guid cardId)
    {
        return await _repository.GetByCardIdAsync(cardId);
    }

    public async Task<IEnumerable<CardTransaction>> GetCardTransactionsForInvoiceAsync(Guid cardId, int month, int year)
    {
        return await _repository.GetByCardIdAndPeriodAsync(cardId, month, year);
    }

    public async Task<CardTransaction?> UpdateCardTransactionAsync(Guid id, UpdateCardTransactionRequest request)
    {
        var cardTransaction = await _repository.GetByIdAsync(id);
        if (cardTransaction == null)
            return null;

        cardTransaction.UpdateDetails(
            request.Description,
            request.Amount,
            request.Date,
            request.Installment
        );

        await _repository.UpdateAsync(cardTransaction);
        return cardTransaction;
    }

    public async Task<bool> DeleteCardTransactionAsync(Guid id)
    {
        var cardTransaction = await _repository.GetByIdAsync(id);
        if (cardTransaction == null)
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }
} 