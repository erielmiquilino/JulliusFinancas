using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Jullius.ServiceApi.Application.Services;

public class FinancialTransactionService
{
    private readonly IFinancialTransactionRepository _repository;
    private readonly ICardRepository _cardRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IServiceProvider _serviceProvider;

    private const string SystemCategoryName = "Cartão";
    private const string SystemCategoryColor = "#9C27B0";

    public FinancialTransactionService(
        IFinancialTransactionRepository repository,
        ICardRepository cardRepository,
        ICategoryRepository categoryRepository,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _cardRepository = cardRepository;
        _categoryRepository = categoryRepository;
        _serviceProvider = serviceProvider;
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
                    request.CategoryId,
                    request.IsPaid,
                    budgetId: request.BudgetId
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
                request.CategoryId,
                request.IsPaid,
                budgetId: request.BudgetId
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

    public async Task<int> DeleteTransactionsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        await _repository.DeleteManyAsync(idList);
        return idList.Count;
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
            request.CategoryId,
            request.IsPaid,
            budgetId: request.BudgetId
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

    public async Task<PayWithCardResponse> PayWithCardAsync(PayWithCardRequest request)
    {
        // Validações básicas
        if (request.TransactionIds == null || !request.TransactionIds.Any())
            throw new ArgumentException("Nenhuma transação foi selecionada");

        if (request.CardAmount <= 0)
            throw new ArgumentException("O valor do cartão deve ser maior que zero");

        // Busca o cartão
        var card = await _cardRepository.GetByIdAsync(request.CardId);
        if (card == null)
            throw new ArgumentException("Cartão não encontrado");

        // Busca as transações selecionadas
        var selectedTransactions = new List<FinancialTransaction>();
        decimal totalAmount = 0;

        foreach (var transactionId in request.TransactionIds)
        {
            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException($"Transação {transactionId} não encontrada");

            selectedTransactions.Add(transaction);
            totalAmount += transaction.Amount;
        }

        // Marca todas as transações selecionadas como pagas
        foreach (var transaction in selectedTransactions)
        {
            transaction.UpdatePaymentStatus(true);
            await _repository.UpdateAsync(transaction);
        }

        // Cria a descrição padrão
        var description = $"Saque do cartão - {card.Name}";

        // Obtém ou cria a categoria de sistema para transações de cartão
        var systemCategory = await _categoryRepository.GetOrCreateSystemCategoryAsync(SystemCategoryName, SystemCategoryColor);

        // Cria a receita (ReceivableBill) com o valor total, já marcada como paga
        var incomeTransaction = new FinancialTransaction(
            description,
            totalAmount,
            DateTime.UtcNow, // Data de vencimento = data atual
            TransactionType.ReceivableBill,
            systemCategory.Id,
            isPaid: true // Já lançada como paga
        );
        var createdIncome = await _repository.CreateAsync(incomeTransaction);

        // Cria a despesa no cartão usando o método existente
        var cardTransactionRequest = new CreateCardTransactionRequest
        {
            CardId = request.CardId,
            Description = description,
            Amount = request.CardAmount,
            Date = DateTime.UtcNow,
            IsInstallment = false,
            InstallmentCount = 1,
            Type = CardTransactionType.Expense,
            InvoiceYear = request.InvoiceYear,
            InvoiceMonth = request.InvoiceMonth
        };

        // Resolve o CardTransactionService sob demanda para evitar dependência circular
        var cardTransactionService = _serviceProvider.GetRequiredService<CardTransactionService>();
        var createdCardTransactions = await cardTransactionService.CreateCardTransactionAsync(cardTransactionRequest);

        return new PayWithCardResponse
        {
            PaidTransactionsCount = selectedTransactions.Count,
            IncomeTransactionId = createdIncome.Id,
            CardTransactionIds = createdCardTransactions.Select(ct => ct.Id).ToList()
        };
    }
} 