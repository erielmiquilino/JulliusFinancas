using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardTransactionService
{
    private readonly ICardTransactionRepository _repository;
    private readonly ICardRepository _cardRepository;

    public CardTransactionService(ICardTransactionRepository repository, ICardRepository cardRepository)
    {
        _repository = repository;
        _cardRepository = cardRepository;
    }

    public async Task<IEnumerable<CardTransaction>> CreateCardTransactionAsync(CreateCardTransactionRequest request)
    {
        var card = await _cardRepository.GetByIdAsync(request.CardId);
        if (card == null)
            throw new ArgumentException("Card not found");

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
                
                // Calcula a qual fatura essa parcela pertence
                var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(installmentDate, card.ClosingDay, card.DueDay);
                
                // Cria o texto da parcela (ex: "1/3", "2/3", "3/3")
                var installmentText = $"{i + 1}/{request.InstallmentCount}";

                var cardTransaction = new CardTransaction(
                    request.CardId,
                    request.Description,
                    installmentAmount,
                    installmentDate,
                    installmentText,
                    invoiceYear,
                    invoiceMonth
                );

                var createdTransaction = await _repository.CreateAsync(cardTransaction);
                transactions.Add(createdTransaction);
            }
        }
        else
        {
            // Calcula a qual fatura essa transação pertence
            var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(request.Date, card.ClosingDay, card.DueDay);
            
            // Cria uma única transação
            var cardTransaction = new CardTransaction(
                request.CardId,
                request.Description,
                request.Amount,
                request.Date,
                "1/1",
                invoiceYear,
                invoiceMonth
            );

            var createdTransaction = await _repository.CreateAsync(cardTransaction);
            transactions.Add(createdTransaction);
        }

        return transactions;
    }

    private (int Year, int Month) CalculateInvoicePeriod(DateTime transactionDate, int closingDay, int dueDay)
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
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay);

        return (invoiceDueDate.Year, invoiceDueDate.Month);
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

        // Busca o cartão para calcular a nova fatura
        var card = await _cardRepository.GetByIdAsync(cardTransaction.CardId);
        if (card == null)
            throw new ArgumentException("Card not found");

        // Calcula a qual fatura essa transação pertence
        var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(request.Date, card.ClosingDay, card.DueDay);

        cardTransaction.UpdateDetails(
            request.Description,
            request.Amount,
            request.Date,
            request.Installment,
            invoiceYear,
            invoiceMonth
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