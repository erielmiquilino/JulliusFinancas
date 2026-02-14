using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class CardTransactionTests
{
    private readonly Guid _validCardId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateCardTransaction()
    {
        // Arrange
        var description = "Compra Amazon";
        var amount = 150.50m;
        var date = DateTime.UtcNow;
        var installment = "1/3";
        var invoiceYear = 2025;
        var invoiceMonth = 6;
        var type = CardTransactionType.Expense;

        // Act
        var transaction = new CardTransaction(_validCardId, description, amount, date, installment, invoiceYear, invoiceMonth, type);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.CardId.Should().Be(_validCardId);
        transaction.Description.Should().Be(description);
        transaction.Amount.Should().Be(amount);
        transaction.Date.Should().Be(date);
        transaction.Installment.Should().Be(installment);
        transaction.InvoiceYear.Should().Be(invoiceYear);
        transaction.InvoiceMonth.Should().Be(invoiceMonth);
        transaction.Type.Should().Be(type);
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithDefaultType_ShouldBeExpense()
    {
        // Act
        var transaction = new CardTransaction(_validCardId, "Compra", 100m, DateTime.UtcNow, "1/1", 2025, 6);

        // Assert
        transaction.Type.Should().Be(CardTransactionType.Expense);
    }

    [Fact]
    public void Constructor_WithEmptyCardId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new CardTransaction(Guid.Empty, "Compra", 100m, DateTime.UtcNow, "1/1", 2025, 6);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("CardId cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyDescription_ShouldThrowArgumentException(string? description)
    {
        // Act
        var act = () => new CardTransaction(_validCardId, description!, 100m, DateTime.UtcNow, "1/1", 2025, 6);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Description cannot be empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal amount)
    {
        // Act
        var act = () => new CardTransaction(_validCardId, "Compra", amount, DateTime.UtcNow, "1/1", 2025, 6);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than zero");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyInstallment_ShouldThrowArgumentException(string? installment)
    {
        // Act
        var act = () => new CardTransaction(_validCardId, "Compra", 100m, DateTime.UtcNow, installment!, 2025, 6);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Installment cannot be empty");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    [InlineData(0)]
    public void Constructor_WithInvalidInvoiceYear_ShouldThrowArgumentException(int invoiceYear)
    {
        // Act
        var act = () => new CardTransaction(_validCardId, "Compra", 100m, DateTime.UtcNow, "1/1", invoiceYear, 6);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invoice year must be valid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(100)]
    public void Constructor_WithInvalidInvoiceMonth_ShouldThrowArgumentException(int invoiceMonth)
    {
        // Act
        var act = () => new CardTransaction(_validCardId, "Compra", 100m, DateTime.UtcNow, "1/1", 2025, invoiceMonth);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invoice month must be between 1 and 12");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateTransaction()
    {
        // Arrange
        var transaction = new CardTransaction(_validCardId, "Compra Original", 100m, DateTime.UtcNow, "1/1", 2025, 6);
        var newDescription = "Compra Atualizada";
        var newAmount = 200m;
        var newDate = DateTime.UtcNow.AddDays(1);
        var newInstallment = "2/3";
        var newInvoiceYear = 2025;
        var newInvoiceMonth = 7;
        var newType = CardTransactionType.Income;

        // Act
        transaction.UpdateDetails(newDescription, newAmount, newDate, newInstallment, newInvoiceYear, newInvoiceMonth, newType);

        // Assert
        transaction.Description.Should().Be(newDescription);
        transaction.Amount.Should().Be(newAmount);
        transaction.Date.Should().Be(newDate);
        transaction.Installment.Should().Be(newInstallment);
        transaction.InvoiceYear.Should().Be(newInvoiceYear);
        transaction.InvoiceMonth.Should().Be(newInvoiceMonth);
        transaction.Type.Should().Be(newType);
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_ShouldThrowArgumentException()
    {
        // Arrange
        var transaction = new CardTransaction(_validCardId, "Compra", 100m, DateTime.UtcNow, "1/1", 2025, 6);

        // Act
        var act = () => transaction.UpdateDetails("", 100m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

