using FluentAssertions;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Xunit;

namespace Jullius.Tests.Services;

public class AutocompleteServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly AutocompleteService _service;

    public AutocompleteServiceTests()
    {
        _mocks = new RepositoryMocks();
        _service = new AutocompleteService(
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CardTransactionRepository.Object
        );
    }

    #region GetDescriptionSuggestionsAsync Tests

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithValidTerm_ShouldReturnSuggestions()
    {
        // Arrange
        var searchTerm = "Droga";
        var financialDescriptions = new[] { "Droga Raia", "Drogasil" };
        var cardDescriptions = new[] { "Drogaria São Paulo" };

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, financialDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, cardDescriptions);

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Droga Raia");
        result.Should().Contain("Drogasil");
        result.Should().Contain("Drogaria São Paulo");
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithShortTerm_ShouldReturnEmpty()
    {
        // Arrange
        var searchTerm = "D";

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithEmptyTerm_ShouldReturnEmpty()
    {
        // Arrange
        var searchTerm = "";

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithNullTerm_ShouldReturnEmpty()
    {
        // Arrange
        string? searchTerm = null;

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithWhitespaceTerm_ShouldReturnEmpty()
    {
        // Arrange
        var searchTerm = "   ";

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_ShouldRemoveDuplicates()
    {
        // Arrange
        var searchTerm = "Super";
        var financialDescriptions = new[] { "Supermercado Extra", "Supermercado Dia" };
        var cardDescriptions = new[] { "Supermercado Extra", "Supermercado Carrefour" }; // "Supermercado Extra" duplicado

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, financialDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, cardDescriptions);

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().HaveCount(3); // Sem duplicatas
        result.Should().Contain("Supermercado Extra");
        result.Should().Contain("Supermercado Dia");
        result.Should().Contain("Supermercado Carrefour");
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_ShouldRemoveDuplicatesCaseInsensitive()
    {
        // Arrange
        var searchTerm = "super";
        var financialDescriptions = new[] { "Supermercado Extra" };
        var cardDescriptions = new[] { "SUPERMERCADO EXTRA" }; // Mesmo texto, case diferente

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, financialDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, cardDescriptions);

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().HaveCount(1); // Apenas um resultado
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_ShouldPrioritizeDescriptionsStartingWithSearchTerm()
    {
        // Arrange
        var searchTerm = "Dro";
        var financialDescriptions = new[] { "Padaria Drogaria", "Droga Raia" };
        var cardDescriptions = new[] { "Drogasil" };

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, financialDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, cardDescriptions);

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(3);
        
        // Descrições que começam com "Dro" devem vir primeiro
        resultList[0].Should().StartWith("Dro");
        resultList[1].Should().StartWith("Dro");
        // "Padaria Drogaria" deve vir por último pois não começa com "Dro"
        resultList[2].Should().Be("Padaria Drogaria");
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_WithNoResults_ShouldReturnEmpty()
    {
        // Arrange
        var searchTerm = "XYZ123";
        var emptyDescriptions = Array.Empty<string>();

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, emptyDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, emptyDescriptions);

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionSuggestionsAsync_ShouldLimitResults()
    {
        // Arrange
        var searchTerm = "Test";
        var manyDescriptions = Enumerable.Range(1, 25).Select(i => $"Test Description {i}").ToArray();

        _mocks.SetupFinancialTransactionDescriptions(searchTerm, manyDescriptions);
        _mocks.SetupCardTransactionDescriptions(searchTerm, Array.Empty<string>());

        // Act
        var result = await _service.GetDescriptionSuggestionsAsync(searchTerm);

        // Assert
        result.Count().Should().BeLessThanOrEqualTo(20);
    }

    #endregion
}

