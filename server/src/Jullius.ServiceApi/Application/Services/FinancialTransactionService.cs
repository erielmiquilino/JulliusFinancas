using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class FinancialTransactionService
{
    private readonly IFinancialTransactionRepository _repository;
    private readonly ICardRepository _cardRepository;

    public FinancialTransactionService(
        IFinancialTransactionRepository repository,
        ICardRepository cardRepository)
    {
        _repository = repository;
        _cardRepository = cardRepository;
    }

    public async Task<IEnumerable<FinancialTransaction>> CreateTransactionAsync(CreateFinancialTransactionRequest request)
    {
        var transactions = new List<FinancialTransaction>();

        if (request.IsInstallment && request.InstallmentCount > 1)
        {
            // Calcula o valor de cada parcela
            var installmentAmount = Math.Round(request.Amount / request.InstallmentCount, 2);
            var remainingAmount = request.Amount - (installmentAmount * (request.InstallmentCount - 1));

            for (int i = 1; i <= request.InstallmentCount; i++)
            {
                // Usa o valor restante na última parcela para evitar diferenças de arredondamento
                var currentAmount = i == request.InstallmentCount ? remainingAmount : installmentAmount;
                
                // Concatena o número da parcela na descrição
                var descriptionWithInstallment = $"{request.Description} ({i:D2}/{request.InstallmentCount:D2})";
                
                // Calcula a data de vencimento (cada parcela é um mês depois da anterior)
                var installmentDueDate = request.DueDate.AddMonths(i - 1);

                var transaction = new FinancialTransaction(
                    descriptionWithInstallment,
                    currentAmount,
                    installmentDueDate,
                    request.Type,
                    request.IsPaid
                );

                var createdTransaction = await _repository.CreateAsync(transaction);
                transactions.Add(createdTransaction);
            }
        }
        else
        {
            // Cria uma única transação
            var transaction = new FinancialTransaction(
                request.Description,
                request.Amount,
                request.DueDate,
                request.Type,
                request.IsPaid
            );

            var createdTransaction = await _repository.CreateAsync(transaction);
            transactions.Add(createdTransaction);
        }

        return transactions;
    }

    public async Task<IEnumerable<FinancialTransaction>> GetAllTransactionsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<FinancialTransaction?> GetTransactionByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<FinancialTransaction?> UpdateTransactionAsync(Guid id, UpdateFinancialTransactionRequest request)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        transaction.UpdateDetails(
            request.Description,
            request.Amount,
            request.DueDate,
            request.Type,
            request.IsPaid
        );

        await _repository.UpdateAsync(transaction);
        return transaction;
    }

    public async Task<FinancialTransaction?> UpdatePaymentStatusAsync(Guid id, bool isPaid)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        // Armazena o status anterior para verificar se houve mudança
        var previousIsPaid = transaction.IsPaid;

        // Atualiza o status de pagamento
        transaction.UpdatePaymentStatus(isPaid);
        await _repository.UpdateAsync(transaction);

        // Se a transação possui CardId vinculado e houve mudança no status de pagamento
        if (transaction.CardId.HasValue && previousIsPaid != isPaid)
        {
            await UpdateCardCurrentLimitForPaymentAsync(transaction.CardId.Value, transaction.Amount, isPaid, previousIsPaid);
        }

        return transaction;
    }

    private async Task UpdateCardCurrentLimitForPaymentAsync(Guid cardId, decimal amount, bool isPaid, bool previousIsPaid)
    {
        var card = await _cardRepository.GetByIdAsync(cardId);
        if (card == null)
            return;

        decimal amountToUpdate = 0;

        // Lógica de atualização do limite baseada na mudança de status
        if (isPaid && !previousIsPaid)
        {
            // Fatura foi paga: soma o valor ao limite atual (aumenta limite disponível)
            amountToUpdate = amount;
        }
        else if (!isPaid && previousIsPaid)
        {
            // Fatura deixou de estar paga: subtrai o valor do limite atual (diminui limite disponível)
            amountToUpdate = -amount;
        }

        // Aplica a mudança no limite atual do cartão
        if (amountToUpdate != 0)
        {
            card.UpdateCurrentLimit(amountToUpdate);
            await _cardRepository.UpdateAsync(card);
        }
    }
} 