using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class CardTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateCard()
    {
        // Arrange
        var name = "Cartão Nubank";
        var issuingBank = "Nubank";
        var closingDay = 15;
        var dueDay = 22;
        var limit = 5000m;

        // Act
        var card = new Card(name, issuingBank, closingDay, dueDay, limit);

        // Assert
        card.Id.Should().NotBeEmpty();
        card.Name.Should().Be(name);
        card.IssuingBank.Should().Be(issuingBank);
        card.ClosingDay.Should().Be(closingDay);
        card.DueDay.Should().Be(dueDay);
        card.Limit.Should().Be(limit);
        card.CurrentLimit.Should().Be(limit);
        card.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException(string? name)
    {
        // Act
        var act = () => new Card(name!, "Nubank", 15, 22, 5000m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyIssuingBank_ShouldThrowArgumentException(string? issuingBank)
    {
        // Act
        var act = () => new Card("Nubank", issuingBank!, 15, 22, 5000m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("IssuingBank cannot be empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    [InlineData(100)]
    public void Constructor_WithInvalidClosingDay_ShouldThrowArgumentException(int closingDay)
    {
        // Act
        var act = () => new Card("Nubank", "Nubank", closingDay, 22, 5000m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ClosingDay must be between 1 and 31");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    [InlineData(100)]
    public void Constructor_WithInvalidDueDay_ShouldThrowArgumentException(int dueDay)
    {
        // Act
        var act = () => new Card("Nubank", "Nubank", 15, dueDay, 5000m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("DueDay must be between 1 and 31");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Constructor_WithInvalidLimit_ShouldThrowArgumentException(decimal limit)
    {
        // Act
        var act = () => new Card("Nubank", "Nubank", 15, 22, limit);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Limit must be greater than zero");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateCard()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        var newName = "Cartão Itaú";
        var newBank = "Itaú";
        var newClosingDay = 10;
        var newDueDay = 17;
        var newLimit = 10000m;

        // Act
        card.UpdateDetails(newName, newBank, newClosingDay, newDueDay, newLimit);

        // Assert
        card.Name.Should().Be(newName);
        card.IssuingBank.Should().Be(newBank);
        card.ClosingDay.Should().Be(newClosingDay);
        card.DueDay.Should().Be(newDueDay);
        card.Limit.Should().Be(newLimit);
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_ShouldThrowArgumentException()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);

        // Act
        var act = () => card.UpdateDetails("", "Itaú", 10, 17, 10000m);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateCurrentLimit_ShouldAddToCurrentLimit()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);

        // Act
        card.UpdateCurrentLimit(-1000m);

        // Assert
        card.CurrentLimit.Should().Be(4000m);
    }

    [Fact]
    public void UpdateCurrentLimit_WithPositiveAmount_ShouldIncreaseLimit()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        card.UpdateCurrentLimit(-2000m); // Simula uso do cartão

        // Act
        card.UpdateCurrentLimit(500m); // Simula pagamento parcial

        // Assert
        card.CurrentLimit.Should().Be(3500m);
    }

    [Fact]
    public void SetCurrentLimit_ShouldSetExactValue()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);

        // Act
        card.SetCurrentLimit(3000m);

        // Assert
        card.CurrentLimit.Should().Be(3000m);
    }
}

