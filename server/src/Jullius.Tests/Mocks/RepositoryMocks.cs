using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Moq;

namespace Jullius.Tests.Mocks;

/// <summary>
/// Classe base para configuração de mocks de repositórios usados nos testes de serviços.
/// </summary>
public class RepositoryMocks
{
    public Mock<ICardRepository> CardRepository { get; }
    public Mock<ICardTransactionRepository> CardTransactionRepository { get; }
    public Mock<IFinancialTransactionRepository> FinancialTransactionRepository { get; }
    public Mock<ICategoryRepository> CategoryRepository { get; }
    public Mock<ICardDescriptionMappingRepository> CardDescriptionMappingRepository { get; }
    public Mock<IBudgetRepository> BudgetRepository { get; }

    public RepositoryMocks()
    {
        CardRepository = new Mock<ICardRepository>();
        CardTransactionRepository = new Mock<ICardTransactionRepository>();
        FinancialTransactionRepository = new Mock<IFinancialTransactionRepository>();
        CategoryRepository = new Mock<ICategoryRepository>();
        CardDescriptionMappingRepository = new Mock<ICardDescriptionMappingRepository>();
        BudgetRepository = new Mock<IBudgetRepository>();

        SetupDefaultBehaviors();
    }

    private void SetupDefaultBehaviors()
    {
        // Card Repository - setup padrão para retornar o mesmo cartão que foi criado
        CardRepository
            .Setup(r => r.CreateAsync(It.IsAny<Card>()))
            .ReturnsAsync((Card card) => card);

        CardRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Card>()))
            .Returns(Task.CompletedTask);

        CardRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // CardTransaction Repository
        CardTransactionRepository
            .Setup(r => r.CreateAsync(It.IsAny<CardTransaction>()))
            .ReturnsAsync((CardTransaction ct) => ct);

        CardTransactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CardTransaction>()))
            .Returns(Task.CompletedTask);

        CardTransactionRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // FinancialTransaction Repository
        FinancialTransactionRepository
            .Setup(r => r.CreateAsync(It.IsAny<FinancialTransaction>()))
            .ReturnsAsync((FinancialTransaction ft) => ft);

        FinancialTransactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FinancialTransaction>()))
            .Returns(Task.CompletedTask);

        FinancialTransactionRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Category Repository
        CategoryRepository
            .Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        CategoryRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        CategoryRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Budget Repository
        BudgetRepository
            .Setup(r => r.CreateAsync(It.IsAny<Budget>()))
            .ReturnsAsync((Budget b) => b);

        BudgetRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        BudgetRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        BudgetRepository
            .Setup(r => r.GetUsedAmountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0m);
    }

    /// <summary>
    /// Configura um cartão para ser retornado quando buscado pelo ID.
    /// </summary>
    public void SetupCardById(Card card)
    {
        CardRepository
            .Setup(r => r.GetByIdAsync(card.Id))
            .ReturnsAsync(card);
    }

    /// <summary>
    /// Configura uma lista de cartões para ser retornada no GetAllAsync.
    /// </summary>
    public void SetupAllCards(IEnumerable<Card> cards)
    {
        CardRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(cards);
    }

    /// <summary>
    /// Configura uma categoria para ser retornada quando buscada pelo ID.
    /// </summary>
    public void SetupCategoryById(Category category)
    {
        CategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id))
            .ReturnsAsync(category);
    }

    /// <summary>
    /// Configura uma lista de categorias para ser retornada no GetAllAsync.
    /// </summary>
    public void SetupAllCategories(IEnumerable<Category> categories)
    {
        CategoryRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);
    }

    /// <summary>
    /// Configura se uma categoria está em uso.
    /// </summary>
    public void SetupCategoryInUse(Guid categoryId, bool isInUse)
    {
        CategoryRepository
            .Setup(r => r.IsInUseAsync(categoryId))
            .ReturnsAsync(isInUse);
    }

    /// <summary>
    /// Configura uma categoria de sistema para ser retornada ou criada.
    /// </summary>
    public void SetupSystemCategory(Category category)
    {
        CategoryRepository
            .Setup(r => r.GetOrCreateSystemCategoryAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(category);
    }

    /// <summary>
    /// Configura uma transação financeira para ser retornada quando buscada pelo ID.
    /// </summary>
    public void SetupFinancialTransactionById(FinancialTransaction transaction)
    {
        FinancialTransactionRepository
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);
    }

    /// <summary>
    /// Configura transações financeiras de um cartão.
    /// </summary>
    public void SetupFinancialTransactionsByCardId(Guid cardId, IEnumerable<FinancialTransaction> transactions)
    {
        FinancialTransactionRepository
            .Setup(r => r.GetByCardIdAsync(cardId))
            .ReturnsAsync(transactions);
    }

    /// <summary>
    /// Configura uma fatura (transação financeira) por cartão e período.
    /// </summary>
    public void SetupInvoiceByCardAndPeriod(Guid cardId, int year, int month, FinancialTransaction? invoice)
    {
        FinancialTransactionRepository
            .Setup(r => r.GetByCardIdAndPeriodAsync(cardId, year, month))
            .ReturnsAsync(invoice);
    }

    /// <summary>
    /// Configura uma transação de cartão para ser retornada quando buscada pelo ID.
    /// </summary>
    public void SetupCardTransactionById(CardTransaction transaction)
    {
        CardTransactionRepository
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);
    }

    /// <summary>
    /// Configura transações de cartão por cartão e período.
    /// </summary>
    public void SetupCardTransactionsByCardAndPeriod(Guid cardId, int month, int year, IEnumerable<CardTransaction> transactions)
    {
        CardTransactionRepository
            .Setup(r => r.GetByCardIdAndPeriodAsync(cardId, month, year))
            .ReturnsAsync(transactions);
    }

    /// <summary>
    /// Configura transações de cartão a partir de um período.
    /// </summary>
    public void SetupCardTransactionsFromPeriod(Guid cardId, int month, int year, IEnumerable<CardTransaction> transactions)
    {
        CardTransactionRepository
            .Setup(r => r.GetByCardIdFromPeriodAsync(cardId, month, year))
            .ReturnsAsync(transactions);
    }

    /// <summary>
    /// Configura transações de cartão por ID do cartão.
    /// </summary>
    public void SetupCardTransactionsByCardId(Guid cardId, IEnumerable<CardTransaction> transactions)
    {
        CardTransactionRepository
            .Setup(r => r.GetByCardIdAsync(cardId))
            .ReturnsAsync(transactions);
    }

    /// <summary>
    /// Configura um budget para ser retornado quando buscado pelo ID.
    /// </summary>
    public void SetupBudgetById(Budget budget)
    {
        BudgetRepository
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);
    }

    /// <summary>
    /// Configura uma lista de budgets para ser retornada no GetAllAsync.
    /// </summary>
    public void SetupAllBudgets(IEnumerable<Budget> budgets)
    {
        BudgetRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(budgets);
    }

    /// <summary>
    /// Configura budgets por mês e ano.
    /// </summary>
    public void SetupBudgetsByMonthAndYear(int month, int year, IEnumerable<Budget> budgets)
    {
        BudgetRepository
            .Setup(r => r.GetByMonthAndYearAsync(month, year))
            .ReturnsAsync(budgets);
    }

    /// <summary>
    /// Configura se um budget está em uso.
    /// </summary>
    public void SetupBudgetInUse(Guid budgetId, bool isInUse)
    {
        BudgetRepository
            .Setup(r => r.IsInUseAsync(budgetId))
            .ReturnsAsync(isInUse);
    }

    /// <summary>
    /// Configura o valor usado de um budget.
    /// </summary>
    public void SetupBudgetUsedAmount(Guid budgetId, decimal usedAmount)
    {
        BudgetRepository
            .Setup(r => r.GetUsedAmountAsync(budgetId))
            .ReturnsAsync(usedAmount);
    }

    /// <summary>
    /// Configura descrições distintas para transações financeiras.
    /// </summary>
    public void SetupFinancialTransactionDescriptions(string searchTerm, IEnumerable<string> descriptions)
    {
        FinancialTransactionRepository
            .Setup(r => r.GetDistinctDescriptionsAsync(searchTerm))
            .ReturnsAsync(descriptions);
    }

    /// <summary>
    /// Configura descrições distintas para transações de cartão.
    /// </summary>
    public void SetupCardTransactionDescriptions(string searchTerm, IEnumerable<string> descriptions)
    {
        CardTransactionRepository
            .Setup(r => r.GetDistinctDescriptionsAsync(searchTerm))
            .ReturnsAsync(descriptions);
    }
}

