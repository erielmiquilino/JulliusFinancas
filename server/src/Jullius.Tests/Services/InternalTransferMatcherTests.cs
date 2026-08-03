using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Xunit;

namespace Jullius.Tests.Services;

public class InternalTransferMatcherTests
{
    private const string Holder = "ERIEL MIQUILINO PEREIRA";

    private readonly InternalTransferMatcher _matcher = new();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _santanderId = Guid.NewGuid();
    private readonly Guid _interId = Guid.NewGuid();

    private ReconciliationItem CreateItem(
        Guid bankAccountId,
        decimal amount,
        DateTime date,
        string description,
        string? category = null,
        string? counterpartyName = null,
        string? counterpartyDocument = null)
    {
        return new ReconciliationItem(
            _sessionId,
            bankAccountId,
            Guid.NewGuid().ToString(),
            description,
            amount,
            date,
            category,
            counterpartyName,
            counterpartyDocument);
    }

    #region Analyze Tests

    [Fact]
    public void Analyze_ShouldPairTransfer_WhenBothLegsArePresent()
    {
        // Arrange — o caso real: PIX de R$ 50 saindo do Santander e entrando no Inter em 01/08.
        var outflow = CreateItem(
            _santanderId, -50.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   Eriel Miquilino Pereira",
            category: "Same person transfer");

        var inflow = CreateItem(
            _interId, 50.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA",
            category: "Transfer - PIX");

        // Act
        var result = _matcher.Analyze([outflow, inflow], [Holder], []);

        // Assert
        result.Pairs.Should().HaveCount(1);
        result.Pairs[0].Outflow.Should().Be(outflow);
        result.Pairs[0].Inflow.Should().Be(inflow);
        result.Orphans.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ShouldReturnOrphan_WhenOnlyOneLegIsPresent()
    {
        // Arrange — PIX para uma conta própria que ainda não está conectada.
        var outflow = CreateItem(
            _santanderId, -120.00m, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   Eriel Miquilino Pereira",
            category: "Same person transfer");

        // Act
        var result = _matcher.Analyze([outflow], [Holder], []);

        // Assert
        result.Pairs.Should().BeEmpty();
        result.Orphans.Should().ContainSingle().Which.Should().Be(outflow);
    }

    [Fact]
    public void Analyze_ShouldNotMatchThirdParty_WhenSurnameIsShared()
    {
        // Arrange — "MARIA APARECIDA MIQUILINO" compartilha o sobrenome mas é terceiro.
        // Um casamento por substring anularia dinheiro real.
        var inflow = CreateItem(
            _interId, 50.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX RECEBIDO - Cp :16501555-50.863.462 MARIA APARECIDA MIQUILINO");

        var outflow = CreateItem(
            _santanderId, -50.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   MARIA APARECIDA MIQUILINO");

        // Act
        var result = _matcher.Analyze([inflow, outflow], [Holder], []);

        // Assert
        result.Pairs.Should().BeEmpty();
        result.Orphans.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ShouldNotPair_WhenBothLegsBelongToTheSameAccount()
    {
        // Arrange — mesma conta não caracteriza transferência entre contas próprias.
        var outflow = CreateItem(
            _santanderId, -75.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   Eriel Miquilino Pereira",
            category: "Same person transfer");

        var inflow = CreateItem(
            _santanderId, 75.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX RECEBIDO   Eriel Miquilino Pereira",
            category: "Same person transfer");

        // Act
        var result = _matcher.Analyze([outflow, inflow], [Holder], []);

        // Assert
        result.Pairs.Should().BeEmpty();
        result.Orphans.Should().HaveCount(2);
    }

    [Fact]
    public void Analyze_ShouldNotPair_WhenDatesAreTooFarApart()
    {
        // Arrange — cinco dias de diferença excede a janela de pareamento de dois dias.
        var outflow = CreateItem(
            _santanderId, -200.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   Eriel Miquilino Pereira",
            category: "Same person transfer");

        var inflow = CreateItem(
            _interId, 200.00m, new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc),
            "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA");

        // Act
        var result = _matcher.Analyze([outflow, inflow], [Holder], []);

        // Assert
        result.Pairs.Should().BeEmpty();
        result.Orphans.Should().HaveCount(2);
    }

    [Fact]
    public void Analyze_ShouldDetectOwnership_ByCounterpartyDocument()
    {
        // Arrange — o Santander devolve paymentData com o CPF do titular.
        const string cpf = "07571532914";

        var outflow = CreateItem(
            _santanderId, -300.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO",
            counterpartyDocument: "075.715.329-14");

        var inflow = CreateItem(
            _interId, 300.00m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX RECEBIDO",
            counterpartyDocument: cpf);

        // Act
        var result = _matcher.Analyze([outflow, inflow], [Holder], [cpf]);

        // Assert
        result.Pairs.Should().HaveCount(1);
        result.Orphans.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ShouldIgnoreRegularExpenses_WhenCounterpartyIsNotTheHolder()
    {
        // Arrange — despesas normais não podem ser confundidas com transferência interna.
        var groceries = CreateItem(
            _santanderId, -93.70m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "PIX ENVIADO   SUPERMERCADOS MYATA LTDA",
            category: "Groceries");

        var salary = CreateItem(
            _santanderId, 6431.03m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "LIQUIDO DE VENCIMENTO   CNPJ 046011836000192",
            category: "Salary");

        // Act
        var result = _matcher.Analyze([groceries, salary], [Holder], []);

        // Assert
        result.Pairs.Should().BeEmpty();
        result.Orphans.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ShouldPairEachTransferOnlyOnce_WhenAmountsRepeat()
    {
        // Arrange — dois PIX de mesmo valor no mesmo dia devem virar dois pares distintos,
        // e não um par com sobra.
        var date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var firstOut = CreateItem(_santanderId, -50m, date, "PIX ENVIADO   Eriel Miquilino Pereira");
        var secondOut = CreateItem(_santanderId, -50m, date, "PIX ENVIADO   Eriel Miquilino Pereira");
        var firstIn = CreateItem(_interId, 50m, date, "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA");
        var secondIn = CreateItem(_interId, 50m, date, "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA");

        // Act
        var result = _matcher.Analyze([firstOut, secondOut, firstIn, secondIn], [Holder], []);

        // Assert
        result.Pairs.Should().HaveCount(2);
        result.Orphans.Should().BeEmpty();
        result.Pairs.SelectMany(pair => new[] { pair.Outflow.Id, pair.Inflow.Id })
            .Should().OnlyHaveUniqueItems();
    }

    #endregion
}
