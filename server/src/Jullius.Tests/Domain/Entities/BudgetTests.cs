using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class BudgetTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateBudget()
    {
        // Arrange
        var name = "Alimentação";
        var limitAmount = 1000m;
        var month = 6;
        var year = 2025;
        var description = "Budget para alimentação";

        // Act
        var budget = new Budget(name, limitAmount, month, year, description);

        // Assert
        budget.Id.Should().NotBeEmpty();
        budget.Name.Should().Be(name);
        budget.LimitAmount.Should().Be(limitAmount);
        budget.Month.Should().Be(month);
        budget.Year.Should().Be(year);
        budget.Description.Should().Be(description);
        budget.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithoutDescription_ShouldCreateBudget()
    {
        // Act
        var budget = new Budget("Lazer", 500m, 1, 2025);

        // Assert
        budget.Name.Should().Be("Lazer");
        budget.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException(string? name)
    {
        // Act
        var act = () => new Budget(name!, 1000m, 6, 2025);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Constructor_WithInvalidLimitAmount_ShouldThrowArgumentException(decimal limitAmount)
    {
        // Act
        var act = () => new Budget("Budget", limitAmount, 6, 2025);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Limit amount must be greater than zero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(14)]
    public void Constructor_WithInvalidMonth_ShouldThrowArgumentException(int month)
    {
        // Act
        var act = () => new Budget("Budget", 1000m, month, 2025);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Month must be between 1 and 12");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Constructor_WithInvalidYear_ShouldThrowArgumentException(int year)
    {
        // Act
        var act = () => new Budget("Budget", 1000m, 6, year);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Year must be between 2000 and 2100");
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateBudget()
    {
        // Arrange
        var budget = new Budget("Budget Original", 1000m, 6, 2025, "Descrição original");
        var newName = "Budget Atualizado";
        var newLimitAmount = 2000m;
        var newMonth = 7;
        var newYear = 2026;
        var newDescription = "Descrição atualizada";

        // Act
        budget.Update(newName, newLimitAmount, newMonth, newYear, newDescription);

        // Assert
        budget.Name.Should().Be(newName);
        budget.LimitAmount.Should().Be(newLimitAmount);
        budget.Month.Should().Be(newMonth);
        budget.Year.Should().Be(newYear);
        budget.Description.Should().Be(newDescription);
    }

    [Fact]
    public void Update_WithInvalidData_ShouldThrowArgumentException()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);

        // Act
        var act = () => budget.Update("", 1000m, 6, 2025);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Constructor_WithValidMonths_ShouldCreateBudget(int month)
    {
        // Act
        var budget = new Budget("Budget", 1000m, month, 2025);

        // Assert
        budget.Month.Should().Be(month);
    }
}

