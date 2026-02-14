using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Xunit;

namespace Jullius.Tests.Domain.Entities;

public class CardDescriptionMappingTests
{
    private readonly Guid _validCardId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateMapping()
    {
        // Arrange
        var originalDescription = "PAGAMENTO*AMAZON";
        var mappedDescription = "Amazon";

        // Act
        var mapping = new CardDescriptionMapping(originalDescription, mappedDescription, _validCardId);

        // Assert
        mapping.Id.Should().NotBeEmpty();
        mapping.OriginalDescription.Should().Be(originalDescription);
        mapping.MappedDescription.Should().Be(mappedDescription);
        mapping.CardId.Should().Be(_validCardId);
        mapping.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        mapping.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOriginalDescription_ShouldThrowArgumentException(string? originalDescription)
    {
        // Act
        var act = () => new CardDescriptionMapping(originalDescription!, "Amazon", _validCardId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OriginalDescription cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyMappedDescription_ShouldThrowArgumentException(string? mappedDescription)
    {
        // Act
        var act = () => new CardDescriptionMapping("PAGAMENTO*AMAZON", mappedDescription!, _validCardId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("MappedDescription cannot be empty");
    }

    [Fact]
    public void Constructor_WithEmptyCardId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new CardDescriptionMapping("PAGAMENTO*AMAZON", "Amazon", Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("CardId cannot be empty");
    }

    [Fact]
    public void Constructor_WithOriginalDescriptionExceeding500Chars_ShouldThrowArgumentException()
    {
        // Arrange
        var longDescription = new string('A', 501);

        // Act
        var act = () => new CardDescriptionMapping(longDescription, "Amazon", _validCardId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OriginalDescription cannot exceed 500 characters");
    }

    [Fact]
    public void Constructor_WithMappedDescriptionExceeding200Chars_ShouldThrowArgumentException()
    {
        // Arrange
        var longDescription = new string('A', 201);

        // Act
        var act = () => new CardDescriptionMapping("PAGAMENTO*AMAZON", longDescription, _validCardId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("MappedDescription cannot exceed 200 characters");
    }

    [Fact]
    public void Constructor_WithOriginalDescriptionExactly500Chars_ShouldSucceed()
    {
        // Arrange
        var exactDescription = new string('A', 500);

        // Act
        var mapping = new CardDescriptionMapping(exactDescription, "Amazon", _validCardId);

        // Assert
        mapping.OriginalDescription.Should().HaveLength(500);
    }

    [Fact]
    public void Constructor_WithMappedDescriptionExactly200Chars_ShouldSucceed()
    {
        // Arrange
        var exactDescription = new string('A', 200);

        // Act
        var mapping = new CardDescriptionMapping("PAGAMENTO*AMAZON", exactDescription, _validCardId);

        // Assert
        mapping.MappedDescription.Should().HaveLength(200);
    }

    [Fact]
    public void UpdateMappedDescription_WithValidData_ShouldUpdateMapping()
    {
        // Arrange
        var mapping = new CardDescriptionMapping("PAGAMENTO*AMAZON", "Amazon", _validCardId);
        var originalUpdatedAt = mapping.UpdatedAt;
        var newMappedDescription = "Amazon Prime";

        // Act
        Thread.Sleep(10); // Ensure time difference
        mapping.UpdateMappedDescription(newMappedDescription);

        // Assert
        mapping.MappedDescription.Should().Be(newMappedDescription);
        mapping.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateMappedDescription_WithEmptyDescription_ShouldThrowArgumentException(string? newDescription)
    {
        // Arrange
        var mapping = new CardDescriptionMapping("PAGAMENTO*AMAZON", "Amazon", _validCardId);

        // Act
        var act = () => mapping.UpdateMappedDescription(newDescription!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("MappedDescription cannot be empty");
    }

    [Fact]
    public void UpdateMappedDescription_WithDescriptionExceeding200Chars_ShouldThrowArgumentException()
    {
        // Arrange
        var mapping = new CardDescriptionMapping("PAGAMENTO*AMAZON", "Amazon", _validCardId);
        var longDescription = new string('A', 201);

        // Act
        var act = () => mapping.UpdateMappedDescription(longDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("MappedDescription cannot exceed 200 characters");
    }
}

