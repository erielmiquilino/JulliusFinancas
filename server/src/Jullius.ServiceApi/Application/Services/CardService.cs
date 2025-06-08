using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardService
{
    private readonly ICardRepository _repository;
    private readonly IFinancialTransactionRepository _financialTransactionRepository;

    public CardService(ICardRepository repository, IFinancialTransactionRepository financialTransactionRepository)
    {
        _repository = repository;
        _financialTransactionRepository = financialTransactionRepository;
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

        card.UpdateDetails(
            request.Name,
            request.IssuingBank,
            request.ClosingDay,
            request.DueDay,
            request.Limit
        );

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
} 