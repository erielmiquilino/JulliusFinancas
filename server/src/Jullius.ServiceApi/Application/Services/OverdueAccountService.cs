using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class OverdueAccountService
{
    private readonly IOverdueAccountRepository _repository;

    public OverdueAccountService(IOverdueAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<OverdueAccountDto> CreateOverdueAccountAsync(CreateOverdueAccountRequest request)
    {
        var overdueAccount = new OverdueAccount(
            request.Description,
            request.CurrentDebtValue
        );
        var created = await _repository.CreateAsync(overdueAccount);
        return MapToDto(created);
    }

    public async Task<IEnumerable<OverdueAccountDto>> GetAllOverdueAccountsAsync()
    {
        var overdueAccounts = await _repository.GetAllAsync();
        return overdueAccounts.Select(MapToDto);
    }

    public async Task<OverdueAccountDto?> GetOverdueAccountByIdAsync(Guid id)
    {
        var overdueAccount = await _repository.GetByIdAsync(id);
        return overdueAccount == null ? null : MapToDto(overdueAccount);
    }

    public async Task<OverdueAccountDto?> UpdateOverdueAccountAsync(Guid id, UpdateOverdueAccountRequest request)
    {
        var overdueAccount = await _repository.GetByIdAsync(id);
        if (overdueAccount == null)
            return null;

        overdueAccount.Update(
            request.Description,
            request.CurrentDebtValue
        );
        await _repository.UpdateAsync(overdueAccount);
        return MapToDto(overdueAccount);
    }

    public async Task<bool> DeleteOverdueAccountAsync(Guid id)
    {
        var overdueAccount = await _repository.GetByIdAsync(id);
        if (overdueAccount == null)
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    private static OverdueAccountDto MapToDto(OverdueAccount overdueAccount)
    {
        return new OverdueAccountDto
        {
            Id = overdueAccount.Id,
            Description = overdueAccount.Description,
            CurrentDebtValue = overdueAccount.CurrentDebtValue,
            CreatedAt = overdueAccount.CreatedAt
        };
    }
}
