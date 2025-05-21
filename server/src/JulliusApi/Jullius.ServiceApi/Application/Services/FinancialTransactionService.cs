using Julius.Domain.Domain.Entities;
using Julius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class FinancialTransactionService(IFinancialTransactionRepository repository)
{
    public async Task<FinancialTransaction> CreateTransactionAsync(CreateFinancialTransactionRequest request)
    {
        var transaction = new FinancialTransaction(
            request.Description,
            request.Amount,
            request.DueDate,
            request.Type
        );

        return await repository.CreateAsync(transaction);
    }

    public async Task<FinancialTransaction?> GetTransactionByIdAsync(Guid id)
    {
        return await repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<FinancialTransaction>> GetAllTransactionsAsync()
    {
        return await repository.GetAllAsync();
    }
} 