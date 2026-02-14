using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class FinancialTransactionTests
{
    private readonly Guid _validCategoryId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateTransaction()
    {
        // Arrange
        var description = "Conta de Luz";
        var amount = 250.00m;
        var dueDate = DateTime.UtcNow.AddDays(30);
        var type = TransactionType.PayableBill;

        // Act
        var transaction = new FinancialTransaction(description, amount, dueDate, type, _validCategoryId);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.Description.Should().Be(description);
        transaction.Amount.Should().Be(amount);
        transaction.DueDate.Should().Be(dueDate);
        transaction.Type.Should().Be(type);
        transaction.CategoryId.Should().Be(_validCategoryId);
        transaction.IsPaid.Should().BeFalse();
        transaction.CardId.Should().BeNull();
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithIsPaidTrue_ShouldCreatePaidTransaction()
    {
        // Act
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId, isPaid: true);

        // Assert
        transaction.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithCardId_ShouldCreateTransactionWithCard()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        // Act
        var transaction = new FinancialTransaction("Fatura", 500m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId, cardId: cardId);

        // Assert
        transaction.CardId.Should().Be(cardId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyDescription_ShouldThrowArgumentException(string? description)
    {
        // Act
        var act = () => new FinancialTransaction(description!, 100m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Description cannot be empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal amount)
    {
        // Act
        var act = () => new FinancialTransaction("Conta", amount, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than zero");
    }

    [Fact]
    public void Constructor_WithEmptyCategoryId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("CategoryId cannot be empty");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateTransaction()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta Original", 100m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId);
        var newDescription = "Conta Atualizada";
        var newAmount = 200m;
        var newDueDate = DateTime.UtcNow.AddDays(15);
        var newType = TransactionType.ReceivableBill;
        var newCategoryId = Guid.NewGuid();
        var newCardId = Guid.NewGuid();

        // Act
        transaction.UpdateDetails(newDescription, newAmount, newDueDate, newType, newCategoryId, true, newCardId);

        // Assert
        transaction.Description.Should().Be(newDescription);
        transaction.Amount.Should().Be(newAmount);
        transaction.DueDate.Should().Be(newDueDate);
        transaction.Type.Should().Be(newType);
        transaction.CategoryId.Should().Be(newCategoryId);
        transaction.IsPaid.Should().BeTrue();
        transaction.CardId.Should().Be(newCardId);
    }

    [Fact]
    public void UpdatePaymentStatus_ToTrue_ShouldMarkAsPaid()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId);

        // Act
        transaction.UpdatePaymentStatus(true);

        // Assert
        transaction.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void UpdatePaymentStatus_ToFalse_ShouldMarkAsUnpaid()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _validCategoryId, isPaid: true);

        // Act
        transaction.UpdatePaymentStatus(false);

        // Assert
        transaction.IsPaid.Should().BeFalse();
    }

    [Theory]
    [InlineData(TransactionType.PayableBill)]
    [InlineData(TransactionType.ReceivableBill)]
    public void Constructor_WithDifferentTypes_ShouldSetCorrectType(TransactionType type)
    {
        // Act
        var transaction = new FinancialTransaction("Transação", 100m, DateTime.UtcNow, type, _validCategoryId);

        // Assert
        transaction.Type.Should().Be(type);
    }
}

