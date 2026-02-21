using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardTransactionService
{
    private readonly ICardTransactionRepository _repository;
    private readonly ICardRepository _cardRepository;
    private readonly IFinancialTransactionRepository _financialTransactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    private const string InvoiceCategoryName = "Fatura de Cartão";
    private const string InvoiceCategoryColor = "#E91E63";

    public CardTransactionService(
        ICardTransactionRepository repository, 
        ICardRepository cardRepository,
        IFinancialTransactionRepository financialTransactionRepository,
        ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _cardRepository = cardRepository;
        _financialTransactionRepository = financialTransactionRepository;
        _categoryRepository = categoryRepository;
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
                
                // Usa o período da fatura recebido do frontend, mas calcula para cada parcela
                var invoiceDate = new DateTime(request.InvoiceYear, request.InvoiceMonth, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(i);
                var invoiceYear = invoiceDate.Year;
                var invoiceMonth = invoiceDate.Month;
                
                // Cria o texto da parcela (ex: "1/3", "2/3", "3/3")
                var installmentText = $"{i + 1}/{request.InstallmentCount}";

                var cardTransaction = new CardTransaction(
                    request.CardId,
                    request.Description,
                    installmentAmount,
                    installmentDate,
                    installmentText,
                    invoiceYear,
                    invoiceMonth,
                    request.Type
                );

                var createdTransaction = await _repository.CreateAsync(cardTransaction);
                transactions.Add(createdTransaction);

                // Atualiza o CurrentLimit do cartão para cada parcela
                await UpdateCardCurrentLimitAsync(card, installmentAmount, request.Type);

                // Cria/atualiza a fatura para cada parcela
                // Se for receita, subtrai da fatura; se for despesa, adiciona
                var amountToApply = request.Type == CardTransactionType.Income ? -installmentAmount : installmentAmount;
                await CreateOrUpdateInvoiceAsync(card, invoiceYear, invoiceMonth, amountToApply);
            }
        }
        else
        {
            // Usa o período da fatura recebido do frontend
            var invoiceYear = request.InvoiceYear;
            var invoiceMonth = request.InvoiceMonth;
            
            // Cria uma única transação
            var cardTransaction = new CardTransaction(
                request.CardId,
                request.Description,
                request.Amount,
                request.Date,
                "1/1",
                invoiceYear,
                invoiceMonth,
                request.Type
            );

            var createdTransaction = await _repository.CreateAsync(cardTransaction);
            transactions.Add(createdTransaction);

            // Atualiza o CurrentLimit do cartão
            await UpdateCardCurrentLimitAsync(card, request.Amount, request.Type);

            // Cria/atualiza a fatura com o valor total da transação
            // Se for receita, subtrai da fatura; se for despesa, adiciona
            var amountToApply = request.Type == CardTransactionType.Income ? -request.Amount : request.Amount;
            await CreateOrUpdateInvoiceAsync(card, invoiceYear, invoiceMonth, amountToApply);
        }

        return transactions;
    }

    private async Task CreateOrUpdateInvoiceAsync(Card card, int invoiceYear, int invoiceMonth, decimal amount)
    {
        var invoiceDescription = $"Fatura {card.Name}";
        
        // Busca se já existe uma fatura para esse cartão no período usando o CardId
        var existingInvoice = await _financialTransactionRepository
            .GetByCardIdAndPeriodAsync(card.Id, invoiceYear, invoiceMonth);

        if (existingInvoice != null)
        {
            // Se já existe, soma o valor no total existente
            var newAmount = existingInvoice.Amount + amount;
            var dueDate = DateTime.SpecifyKind(new DateTime(invoiceYear, invoiceMonth, card.DueDay), DateTimeKind.Utc);
            
            existingInvoice.UpdateDetails(
                invoiceDescription,
                newAmount,
                dueDate,
                TransactionType.PayableBill,
                existingInvoice.CategoryId,
                false, // isPaid sempre false
                card.Id // cardId
            );

            await _financialTransactionRepository.UpdateAsync(existingInvoice);
        }
        else
        {
            // Obtém ou cria a categoria de sistema para faturas de cartão
            var invoiceCategory = await _categoryRepository.GetOrCreateSystemCategoryAsync(InvoiceCategoryName, InvoiceCategoryColor);
            
            // Se não existe, cria uma nova fatura
            var dueDate = DateTime.SpecifyKind(new DateTime(invoiceYear, invoiceMonth, card.DueDay), DateTimeKind.Utc);
            
            var newInvoice = new FinancialTransaction(
                invoiceDescription,
                amount,
                dueDate,
                TransactionType.PayableBill,
                invoiceCategory.Id,
                false, // isPaid sempre false
                card.Id // cardId
            );

            await _financialTransactionRepository.CreateAsync(newInvoice);
        }
    }

    private async Task UpdateInvoiceAmountAsync(Card card, int invoiceYear, int invoiceMonth, decimal amountChange)
    {
        var invoiceDescription = $"Fatura {card.Name}";
        
        // Busca a fatura existente usando o CardId
        var existingInvoice = await _financialTransactionRepository
            .GetByCardIdAndPeriodAsync(card.Id, invoiceYear, invoiceMonth);

        if (existingInvoice != null)
        {
            var newAmount = existingInvoice.Amount + amountChange;
            
            // Se o novo valor for zero ou negativo, remove a fatura
            if (newAmount <= 0)
            {
                await _financialTransactionRepository.DeleteAsync(existingInvoice.Id);
            }
            else
            {
                // Atualiza o valor da fatura
                var dueDate = DateTime.SpecifyKind(new DateTime(invoiceYear, invoiceMonth, card.DueDay), DateTimeKind.Utc);
                
                existingInvoice.UpdateDetails(
                    invoiceDescription,
                    newAmount,
                    dueDate,
                    TransactionType.PayableBill,
                    existingInvoice.CategoryId,
                    false, // isPaid sempre false
                    card.Id // cardId
                );

                await _financialTransactionRepository.UpdateAsync(existingInvoice);
            }
        }
    }

    private (int Year, int Month) CalculateInvoicePeriod(DateTime transactionDate, int closingDay, int dueDay)
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
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay, 0, 0, 0, DateTimeKind.Utc);

        return (invoiceDueDate.Year, invoiceDueDate.Month);
    }

    private async Task UpdateCardCurrentLimitAsync(Card card, decimal amount, CardTransactionType type)
    {
        // Para receitas (Income), soma ao current limit (aumenta limite disponível)
        // Para despesas (Expense), subtrai do current limit (diminui limite disponível)
        var amountToUpdate = type == CardTransactionType.Income ? amount : -amount;
        
        card.UpdateCurrentLimit(amountToUpdate);
        await _cardRepository.UpdateAsync(card);
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

    public async Task<CardInvoiceResponse> GetCardTransactionsForInvoiceAsync(Guid cardId, int month, int year)
    {
        // Busca as transações da fatura
        var transactions = await _repository.GetByCardIdAndPeriodAsync(cardId, month, year);
        
        // Busca o cartão para obter informações atuais
        var card = await _cardRepository.GetByIdAsync(cardId);
        if (card == null)
            throw new ArgumentException("Card not found");

        // Calcula o total da fatura (despesas positivas, receitas negativas)
        var invoiceTotal = transactions.Sum(t => t.Type == CardTransactionType.Expense ? t.Amount : -t.Amount);

        return new CardInvoiceResponse
        {
            Transactions = transactions,
            CurrentLimit = card.CurrentLimit,
            InvoiceTotal = invoiceTotal,
            CardName = card.Name,
            Month = month,
            Year = year
        };
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

        // Guarda os valores antigos para reverter da fatura anterior
        var oldAmount = cardTransaction.Amount;
        var oldType = cardTransaction.Type;
        var oldInvoiceYear = cardTransaction.InvoiceYear;
        var oldInvoiceMonth = cardTransaction.InvoiceMonth;

        // Usa o período da fatura recebido do frontend
        var invoiceYear = request.InvoiceYear;
        var invoiceMonth = request.InvoiceMonth;

        cardTransaction.UpdateDetails(
            request.Description,
            request.Amount,
            request.Date,
            request.Installment,
            invoiceYear,
            invoiceMonth,
            request.Type
        );

        await _repository.UpdateAsync(cardTransaction);

        // Atualiza o CurrentLimit do cartão
        // Primeiro reverte o valor antigo
        var oldAmountToRevert = oldType == CardTransactionType.Income ? -oldAmount : oldAmount;
        card.UpdateCurrentLimit(oldAmountToRevert);
        
        // Depois aplica o novo valor
        await UpdateCardCurrentLimitAsync(card, request.Amount, request.Type);

        // Remove o valor antigo da fatura anterior (considerando o tipo antigo)
        var oldAmountToRevertFromInvoice = oldType == CardTransactionType.Income ? oldAmount : -oldAmount;
        await UpdateInvoiceAmountAsync(card, oldInvoiceYear, oldInvoiceMonth, oldAmountToRevertFromInvoice);

        // Adiciona o novo valor na fatura nova/atual (considerando o novo tipo)
        var newAmountToApply = request.Type == CardTransactionType.Income ? -request.Amount : request.Amount;
        await CreateOrUpdateInvoiceAsync(card, invoiceYear, invoiceMonth, newAmountToApply);

        return cardTransaction;
    }

    public async Task<bool> DeleteCardTransactionAsync(Guid id)
    {
        var cardTransaction = await _repository.GetByIdAsync(id);
        if (cardTransaction == null)
            return false;

        // Busca o cartão para calcular a fatura
        var card = await _cardRepository.GetByIdAsync(cardTransaction.CardId);
        if (card == null)
            throw new ArgumentException("Card not found");

        // Reverte o valor do CurrentLimit do cartão (operação inversa)
        var amountToRevertFromLimit = cardTransaction.Type == CardTransactionType.Income ? -cardTransaction.Amount : cardTransaction.Amount;
        card.UpdateCurrentLimit(amountToRevertFromLimit);
        await _cardRepository.UpdateAsync(card);

        // Remove o valor da fatura antes de excluir a transação (considerando o tipo)
        var amountToRevert = cardTransaction.Type == CardTransactionType.Income ? cardTransaction.Amount : -cardTransaction.Amount;
        await UpdateInvoiceAmountAsync(card, cardTransaction.InvoiceYear, cardTransaction.InvoiceMonth, amountToRevert);

        await _repository.DeleteAsync(id);
        return true;
    }
} 