using Jullius.Domain.Domain.Repositories;

namespace Jullius.ServiceApi.Application.Services;

public class AutocompleteService
{
    private readonly IFinancialTransactionRepository _financialTransactionRepository;
    private readonly ICardTransactionRepository _cardTransactionRepository;

    public AutocompleteService(
        IFinancialTransactionRepository financialTransactionRepository,
        ICardTransactionRepository cardTransactionRepository)
    {
        _financialTransactionRepository = financialTransactionRepository;
        _cardTransactionRepository = cardTransactionRepository;
    }

    public async Task<IEnumerable<string>> GetDescriptionSuggestionsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return Enumerable.Empty<string>();

        // Busca descrições em ambas as tabelas
        var financialDescriptions = await _financialTransactionRepository.GetDistinctDescriptionsAsync(searchTerm);
        var cardDescriptions = await _cardTransactionRepository.GetDistinctDescriptionsAsync(searchTerm);

        // Combina e remove duplicatas (case-insensitive), ordenando por relevância
        var allDescriptions = financialDescriptions
            .Concat(cardDescriptions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 : 1) // Prioriza descrições que começam com o termo
            .ThenBy(d => d)
            .Take(20);

        return allDescriptions;
    }
}

