using FluentAssertions;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Xunit;

namespace Jullius.Tests.Services;

public class BankStatementNormalizerTests
{
    #region ToLedgerDate Tests

    [Fact]
    public void ToLedgerDate_ShouldKeepTheSameDay_WhenPluggySendsLocalMidnightAsUtc()
    {
        // Arrange — a Pluggy converte a meia-noite de Brasília para 03:00Z.
        var pluggyDate = new DateTime(2026, 7, 11, 3, 0, 0, DateTimeKind.Utc);

        // Act
        var result = BankStatementNormalizer.ToLedgerDate(pluggyDate);

        // Assert
        result.Should().Be(new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ToLedgerDate_ShouldKeepTheSameDay_WhenTransactionHappenedAtDawn()
    {
        // Arrange — 05:55Z equivale a 02:55 em Brasília, ainda no dia 03.
        var pluggyDate = new DateTime(2026, 8, 3, 5, 55, 55, DateTimeKind.Utc);

        // Act
        var result = BankStatementNormalizer.ToLedgerDate(pluggyDate);

        // Assert
        result.Should().Be(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToLedgerDate_ShouldMoveToPreviousDay_WhenUtcAlreadyRolledOver()
    {
        // Arrange — 02:00Z do dia 04 ainda é 23:00 do dia 03 em Brasília.
        // Sem a conversão de fuso, a compra cairia no mês errado numa virada de mês.
        var pluggyDate = new DateTime(2026, 8, 4, 2, 0, 0, DateTimeKind.Utc);

        // Act
        var result = BankStatementNormalizer.ToLedgerDate(pluggyDate);

        // Assert
        result.Should().Be(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region ToCalendarDate Tests

    [Fact]
    public void ToCalendarDate_ShouldKeepTheChosenDay_WhenUserPicksMidnightUtc()
    {
        // Arrange — data escolhida no datepicker. Aplicar conversão de fuso aqui jogaria
        // 01/08 de volta para 31/07 e o sync começaria um dia antes do pretendido.
        var picked = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = BankStatementNormalizer.ToCalendarDate(picked);

        // Assert
        result.Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToCalendarDate_ShouldKeepTheChosenDay_WhenDatepickerSendsLocalMidnight()
    {
        // Arrange — o datepicker do Angular serializa a meia-noite local como 03:00Z.
        var picked = new DateTime(2026, 7, 31, 3, 0, 0, DateTimeKind.Utc);

        // Act
        var result = BankStatementNormalizer.ToCalendarDate(picked);

        // Assert
        result.Should().Be(new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region ExtractCounterparty Tests

    [Fact]
    public void ExtractCounterparty_ShouldReturnCnpjRootAndName_WhenDescriptionUsesInterFormat()
    {
        // Arrange
        const string description = "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA";

        // Act
        var (cnpjRoot, name) = BankStatementNormalizer.ExtractCounterparty(description);

        // Assert
        cnpjRoot.Should().Be("90400888");
        name.Should().Be("ERIEL MIQUILINO PEREIRA");
    }

    [Fact]
    public void ExtractCounterparty_ShouldReturnMerchantName_WhenCounterpartyIsACompany()
    {
        // Arrange
        const string description = "PIX ENVIADO - Cp :60701190-IFOOD COM AGENCIA DE RESTAURA";

        // Act
        var (cnpjRoot, name) = BankStatementNormalizer.ExtractCounterparty(description);

        // Assert
        cnpjRoot.Should().Be("60701190");
        name.Should().Be("IFOOD COM AGENCIA DE RESTAURA");
    }

    [Fact]
    public void ExtractCounterparty_ShouldReturnNulls_WhenDescriptionHasNoCounterpartyBlock()
    {
        // Arrange — o Santander não usa esse formato.
        const string description = "PIX ENVIADO   SUPERMERCADOS MYATA LTDA";

        // Act
        var (cnpjRoot, name) = BankStatementNormalizer.ExtractCounterparty(description);

        // Assert
        cnpjRoot.Should().BeNull();
        name.Should().BeNull();
    }

    #endregion

    #region OnlyDigits Tests

    [Fact]
    public void OnlyDigits_ShouldStripFormatting_WhenDocumentIsMasked()
    {
        // Arrange & Act
        var result = BankStatementNormalizer.OnlyDigits("075.715.329-14");

        // Assert
        result.Should().Be("07571532914");
    }

    [Fact]
    public void OnlyDigits_ShouldReturnEmpty_WhenValueIsNull()
    {
        // Arrange & Act
        var result = BankStatementNormalizer.OnlyDigits(null);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ToLedgerDescription Tests

    [Fact]
    public void ToLedgerDescription_ShouldCollapseSpaces_WhenBankPadsColumns()
    {
        // Arrange — os bancos alinham colunas com espaços múltiplos.
        const string raw = "PIX ENVIADO   IFOOD COM AGENCIA DE REST";

        // Act
        var result = BankStatementNormalizer.ToLedgerDescription(raw);

        // Assert
        result.Should().Be("PIX ENVIADO IFOOD COM AGENCIA DE REST");
    }

    [Fact]
    public void ToLedgerDescription_ShouldTruncate_WhenDescriptionExceedsColumnLimit()
    {
        // Arrange — FinancialTransaction.Description aceita no máximo 200 caracteres.
        var raw = new string('A', 260);

        // Act
        var result = BankStatementNormalizer.ToLedgerDescription(raw);

        // Assert
        result.Should().HaveLength(200);
    }

    [Fact]
    public void ToLedgerDescription_ShouldReturnFallback_WhenDescriptionIsEmpty()
    {
        // Arrange & Act — a entidade rejeita descrição vazia.
        var result = BankStatementNormalizer.ToLedgerDescription("   ");

        // Assert
        result.Should().Be("Lançamento sem descrição");
    }

    #endregion
}
