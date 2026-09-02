using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Xunit;

namespace Jullius.Tests.Services;

/// <summary>
/// Casos extraídos do extrato real: o ranqueamento precisa achar o lançamento equivalente
/// sem confundir coincidências de valor entre coisas distintas.
/// </summary>
public class TransactionMatchFinderTests
{
    private static readonly DateTime Dia = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly TransactionMatchFinder _finder = new();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Category _categoria = new("Essenciais", "#FF5722");

    private ReconciliationItem Item(decimal amount, string description, DateTime? date = null) =>
        new(_sessionId, _accountId, Guid.NewGuid().ToString(), description, amount, date ?? Dia);

    private FinancialTransaction Lancamento(
        string description, decimal amount, DateTime dueDate, bool isPaid = true,
        TransactionType type = TransactionType.PayableBill) =>
        new(description, amount, dueDate, type, _categoria.Id, isPaid);

    #region Find Tests

    [Fact]
    public void Find_ShouldRankExactMatchFirst_WhenAmountDateAndTextAgree()
    {
        // Arrange — caso real: "Pagamento GOOGLE BRASIL" x "Google Drive - Anual - 200GB".
        var item = Item(-149.90m, "Pagamento GOOGLE BRASIL PAGAMENTOS LTDA");
        var ledger = new[]
        {
            Lancamento("Google Drive - Anual - 200GB", 149.90m, Dia),
            Lancamento("Aluguel", 1500.00m, Dia.AddDays(4))
        };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert
        result.Should().NotBeEmpty();
        result[0].Transaction.Description.Should().Be("Google Drive - Anual - 200GB");
        result[0].IsStrong.Should().BeTrue();
        result[0].Reasons.Should().Contain("valor idêntico");
    }

    [Fact]
    public void Find_ShouldNotSuggestStrongly_WhenOnlyAmountAndDateCoincide()
    {
        // Arrange — o falso positivo real: a pizzaria e o investimento custam R$ 195,00
        // no mesmo dia, mas não têm nada a ver um com o outro.
        var item = Item(-195.00m, "Pagamento com QR Pix PIZZARIA DUOS LTDA");
        var ledger = new[] { Lancamento("Investimento em limite de crédito Inter", 195.00m, Dia) };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert — aparece como opção, mas não com força de sugestão automática.
        result.Should().ContainSingle();
        result[0].IsStrong.Should().BeFalse();
    }

    [Fact]
    public void Find_ShouldSuggestUpdatingAmount_WhenLedgerHoldsAnEstimate()
    {
        // Arrange — projeção de R$ 1.700,00 que o banco cobrou como R$ 1.752,60.
        var item = Item(-1752.60m, "Pagamento com QR Pix Banco Digio");
        var ledger = new[] { Lancamento("Digio - quitação", 1700.00m, Dia.AddDays(14), isPaid: false) };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert
        result.Should().ContainSingle();
        result[0].SuggestUpdateAmount.Should().BeTrue();
        result[0].SuggestMarkAsPaid.Should().BeTrue();
        result[0].SuggestUpdateDueDate.Should().BeTrue();
    }

    [Fact]
    public void Find_ShouldRecognizeTheSum_WhenAnotherLineAlreadyPointsAtTheSameTransaction()
    {
        // Arrange — as duas cobranças da Juvo (200,23 + 194,48) fecham a parcela de 394,71.
        var lancamento = Lancamento("Juvo Crédito (09 e 10 de 12)", 394.71m, Dia);
        var item = Item(-194.48m, "PIX ENVIADO Juvo Brasil Tecnologia Ltda");
        var jaVinculado = new Dictionary<Guid, decimal> { [lancamento.Id] = 200.23m };

        // Act
        var result = _finder.Find(item, new[] { lancamento }, jaVinculado);

        // Assert
        result.Should().ContainSingle();
        result[0].CombinedAmount.Should().Be(394.71m);
        result[0].Reasons.Should().Contain("soma com o item já vinculado fecha o valor");
        result[0].IsStrong.Should().BeTrue();
        result[0].SuggestUpdateAmount.Should().BeFalse("a soma já bate com o valor lançado");
    }

    [Fact]
    public void Find_ShouldIgnoreOppositeDirection_WhenTypesDiffer()
    {
        // Arrange — uma entrada nunca pode ser vinculada a uma despesa de mesmo valor.
        var item = Item(6431.03m, "LIQUIDO DE VENCIMENTO");
        var ledger = new[] { Lancamento("Aluguel do ano", 6431.03m, Dia) };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Find_ShouldMatchAcrossDays_WhenPaymentLandedBeforeTheDueDate()
    {
        // Arrange — Jeitto: banco em 01/09, lançamento manual em 08/09.
        var item = Item(-715.03m, "Pagamento com QR Pix JEITTO FUNDO DE INVESTIMENTO");
        var ledger = new[] { Lancamento("Jeitto - quitação", 715.03m, Dia.AddDays(7)) };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert
        result.Should().ContainSingle();
        result[0].SuggestUpdateDueDate.Should().BeTrue();
        result[0].SuggestUpdateAmount.Should().BeFalse();
    }

    [Fact]
    public void Find_ShouldReturnEmpty_WhenNothingIsCloseEnough()
    {
        // Arrange
        var item = Item(-104.60m, "Pagamento com QR Pix LOJAS MILIUM LTDA");
        var ledger = new[] { Lancamento("Aluguel", 1500.00m, Dia) };

        // Act
        var result = _finder.Find(item, ledger, new Dictionary<Guid, decimal>());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
