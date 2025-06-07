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

    public async Task<IEnumerable<CardTransaction>> CreateCardTransactionAsync(CreateCardTransactionRequest request)
    {
        var transactions = new List<CardTransaction>();

        if (request.IsInstallment && request.InstallmentCount > 1)
        {
            // Calcula o valor de cada parcela
            var installmentAmount = Math.Round(request.Amount / request.InstallmentCount, 2);
            
            // Cria múltiplas transações parceladas
            for (int i = 0; i < request.InstallmentCount; i++)
            {
                // Calcula a data de cada parcela (adiciona meses)
                var installmentDate = request.Date.AddMonths(i);
                
                // Cria o texto da parcela (ex: "1/3", "2/3", "3/3")
                var installmentText = $"{i + 1}/{request.InstallmentCount}";

                var cardTransaction = new CardTransaction(
                    request.CardId,
                    request.Description,
                    installmentAmount,
                    installmentDate,
                    installmentText
                );

                var createdTransaction = await _repository.CreateAsync(cardTransaction);
                transactions.Add(createdTransaction);
            }
        }
        else
        {
            // Cria uma única transação
            var cardTransaction = new CardTransaction(
                request.CardId,
                request.Description,
                request.Amount,
                request.Date,
                "1/1"
            );

            var createdTransaction = await _repository.CreateAsync(cardTransaction);
            transactions.Add(createdTransaction);
        }

        return transactions;
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