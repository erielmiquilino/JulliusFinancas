using Julius.Domain.Domain.Entities;
using Julius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class FinancialTransactionService
{
    private readonly IFinancialTransactionRepository _repository;

    public FinancialTransactionService(IFinancialTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<FinancialTransaction> CreateTransactionAsync(CreateFinancialTransactionRequest request)
    {
        var transaction = new FinancialTransaction(
            request.Description,
            request.Amount,
            request.DueDate,
            request.Type
        );

        await _repository.CreateAsync(transaction);
        return transaction;
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

        transaction.UpdatePaymentStatus(isPaid);
        await _repository.UpdateAsync(transaction);
        return transaction;
    }
} 