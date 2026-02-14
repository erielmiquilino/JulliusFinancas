using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class BudgetService
{
    private readonly IBudgetRepository _repository;

    public BudgetService(IBudgetRepository repository)
    {
        _repository = repository;
    }

    public async Task<BudgetDto> CreateBudgetAsync(CreateBudgetRequest request)
    {
        var budget = new Budget(
            request.Name,
            request.LimitAmount,
            request.Month,
            request.Year,
            request.Description
        );
        var created = await _repository.CreateAsync(budget);
        return await MapToDtoAsync(created);
    }

    public async Task<IEnumerable<BudgetDto>> GetAllBudgetsAsync()
    {
        var budgets = await _repository.GetAllAsync();
        var dtos = new List<BudgetDto>();
        foreach (var budget in budgets)
        {
            dtos.Add(await MapToDtoAsync(budget));
        }
        return dtos;
    }

    public async Task<IEnumerable<BudgetDto>> GetBudgetsByMonthAndYearAsync(int month, int year)
    {
        var budgets = await _repository.GetByMonthAndYearAsync(month, year);
        var dtos = new List<BudgetDto>();
        foreach (var budget in budgets)
        {
            dtos.Add(await MapToDtoAsync(budget));
        }
        return dtos;
    }

    public async Task<BudgetDto?> GetBudgetByIdAsync(Guid id)
    {
        var budget = await _repository.GetByIdAsync(id);
        return budget == null ? null : await MapToDtoAsync(budget);
    }

    public async Task<BudgetDto?> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request)
    {
        var budget = await _repository.GetByIdAsync(id);
        if (budget == null)
            return null;

        budget.Update(
            request.Name,
            request.LimitAmount,
            request.Month,
            request.Year,
            request.Description
        );
        await _repository.UpdateAsync(budget);
        return await MapToDtoAsync(budget);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteBudgetAsync(Guid id)
    {
        var budget = await _repository.GetByIdAsync(id);
        if (budget == null)
            return (false, "Budget não encontrado");

        var isInUse = await _repository.IsInUseAsync(id);
        if (isInUse)
            return (false, "Não é possível excluir um budget que possui transações vinculadas");

        await _repository.DeleteAsync(id);
        return (true, null);
    }

    public async Task<decimal> GetUsedAmountAsync(Guid budgetId)
    {
        return await _repository.GetUsedAmountAsync(budgetId);
    }

    private async Task<BudgetDto> MapToDtoAsync(Budget budget)
    {
        var usedAmount = await _repository.GetUsedAmountAsync(budget.Id);
        return new BudgetDto
        {
            Id = budget.Id,
            Name = budget.Name,
            LimitAmount = budget.LimitAmount,
            Description = budget.Description,
            Month = budget.Month,
            Year = budget.Year,
            CreatedAt = budget.CreatedAt,
            UsedAmount = usedAmount
        };
    }
}

