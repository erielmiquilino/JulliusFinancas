using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardTransactionService
{
    private readonly ICardTransactionRepository _repository;
    private readonly ICardRepository _cardRepository;
    private readonly IFinancialTransactionRepository _financialTransactionRepository;

    public CardTransactionService(
        ICardTransactionRepository repository, 
        ICardRepository cardRepository,
        IFinancialTransactionRepository financialTransactionRepository)
    {
        _repository = repository;
        _cardRepository = cardRepository;
        _financialTransactionRepository = financialTransactionRepository;
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
                    invoiceMonth,
                    request.Type
                );

                var createdTransaction = await _repository.CreateAsync(cardTransaction);
                transactions.Add(createdTransaction);

                // Cria/atualiza a fatura para cada parcela
                // Se for receita, subtrai da fatura; se for despesa, adiciona
                var amountToApply = request.Type == CardTransactionType.Income ? -installmentAmount : installmentAmount;
                await CreateOrUpdateInvoiceAsync(card, invoiceYear, invoiceMonth, amountToApply);
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
                invoiceMonth,
                request.Type
            );

            var createdTransaction = await _repository.CreateAsync(cardTransaction);
            transactions.Add(createdTransaction);

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
        
        // Busca se já existe uma fatura para esse cartão no período
        var existingInvoice = await _financialTransactionRepository
            .GetByDescriptionAndPeriodAsync(invoiceDescription, invoiceYear, invoiceMonth);

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
                false // isPaid sempre false
            );

            await _financialTransactionRepository.UpdateAsync(existingInvoice);
        }
        else
        {
            // Se não existe, cria uma nova fatura
            var dueDate = DateTime.SpecifyKind(new DateTime(invoiceYear, invoiceMonth, card.DueDay), DateTimeKind.Utc);
            
            var newInvoice = new FinancialTransaction(
                invoiceDescription,
                amount,
                dueDate,
                TransactionType.PayableBill,
                false // isPaid sempre false
            );

            await _financialTransactionRepository.CreateAsync(newInvoice);
        }
    }

    private async Task UpdateInvoiceAmountAsync(Card card, int invoiceYear, int invoiceMonth, decimal amountChange)
    {
        var invoiceDescription = $"Fatura {card.Name}";
        
        // Busca a fatura existente
        var existingInvoice = await _financialTransactionRepository
            .GetByDescriptionAndPeriodAsync(invoiceDescription, invoiceYear, invoiceMonth);

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
                    false // isPaid sempre false
                );

                await _financialTransactionRepository.UpdateAsync(existingInvoice);
            }
        }
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

        // Guarda os valores antigos para reverter da fatura anterior
        var oldAmount = cardTransaction.Amount;
        var oldType = cardTransaction.Type;
        var oldInvoiceYear = cardTransaction.InvoiceYear;
        var oldInvoiceMonth = cardTransaction.InvoiceMonth;

        // Calcula a qual fatura essa transação pertence
        var (invoiceYear, invoiceMonth) = CalculateInvoicePeriod(request.Date, card.ClosingDay, card.DueDay);

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

        // Remove o valor antigo da fatura anterior (considerando o tipo antigo)
        var oldAmountToRevert = oldType == CardTransactionType.Income ? oldAmount : -oldAmount;
        await UpdateInvoiceAmountAsync(card, oldInvoiceYear, oldInvoiceMonth, oldAmountToRevert);

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

        // Remove o valor da fatura antes de excluir a transação (considerando o tipo)
        var amountToRevert = cardTransaction.Type == CardTransactionType.Income ? cardTransaction.Amount : -cardTransaction.Amount;
        await UpdateInvoiceAmountAsync(card, cardTransaction.InvoiceYear, cardTransaction.InvoiceMonth, amountToRevert);

        await _repository.DeleteAsync(id);
        return true;
    }
} 