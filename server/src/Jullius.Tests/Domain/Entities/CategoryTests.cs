using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateCategory()
    {
        // Arrange
        var name = "Alimentação";
        var color = "#FF5722";

        // Act
        var category = new Category(name, color);

        // Assert
        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be(name);
        category.Color.Should().Be(color);
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException(string? name)
    {
        // Act
        var act = () => new Category(name!, "#FF5722");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyColor_ShouldThrowArgumentException(string? color)
    {
        // Act
        var act = () => new Category("Alimentação", color!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Color cannot be empty");
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        var newName = "Transporte";
        var newColor = "#2196F3";

        // Act
        category.Update(newName, newColor);

        // Assert
        category.Name.Should().Be(newName);
        category.Color.Should().Be(newColor);
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");

        // Act
        var act = () => category.Update("", "#2196F3");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    [Fact]
    public void Update_WithEmptyColor_ShouldThrowArgumentException()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");

        // Act
        var act = () => category.Update("Transporte", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Color cannot be empty");
    }
}

