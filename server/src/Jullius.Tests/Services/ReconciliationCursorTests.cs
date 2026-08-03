using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Xunit;

namespace Jullius.Tests.Services;

/// <summary>
/// O cursor define de onde cada sincronização parte. Errar para frente perde lançamento;
/// errar para trás só reimporta o que o dedupe descarta.
/// </summary>
public class ReconciliationCursorTests
{
    private static BankAccount CreateAccount() =>
        new("Conta Corrente", "Banco Inter", "ERIEL MIQUILINO PEREIRA", "item-1", "account-1");

    #region ResolveStartDate Tests

    [Fact]
    public void ResolveStartDate_ShouldUseRequestedDate_WhenAccountWasNeverSynced()
    {
        // Arrange — primeiro sync do projeto, com base em 01/08/2026.
        var account = CreateAccount();
        var requested = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = ReconciliationService.ResolveStartDate(account, requested);

        // Assert
        result.Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveStartDate_ShouldStartAfterOpeningBalance_WhenMarcoZeroWasJustDefined()
    {
        // Arrange — o marco zero grava o cursor no dia seguinte à abertura,
        // porque o movimento do dia 31/07 já está embutido no saldo anterior.
        var account = CreateAccount();
        account.SetOpeningBalance(-194.91m, new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());
        account.RegisterSync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = ReconciliationService.ResolveStartDate(account, null);

        // Assert
        result.Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveStartDate_ShouldReprocessTheSameDay_WhenAccountWasAlreadySynced()
    {
        // Arrange — sincronizado hoje às 14h.
        var account = CreateAccount();
        account.RegisterSync(new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc));

        // Act
        var result = ReconciliationService.ResolveStartDate(account, null);

        // Assert — volta ao início do próprio dia para não perder o que entrou depois das 14h.
        // Reimportar é inofensivo: o ExternalId é único e o dedupe descarta o repetido.
        result.Should().Be(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveStartDate_ShouldIgnoreRequestedDate_WhenAccountAlreadyHasCursor()
    {
        // Arrange — a data pedida só vale no primeiro sync; depois o cursor da conta manda,
        // senão um pedido antigo reabriria período já conciliado.
        var account = CreateAccount();
        account.RegisterSync(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = ReconciliationService.ResolveStartDate(account, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        result.Should().Be(new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region BankAccount Opening Balance Tests

    [Fact]
    public void SetOpeningBalance_ShouldAcceptNegativeValue_WhenAccountIsUsingOverdraft()
    {
        // Arrange — o Inter fecha julho em -194,91 usando cheque especial.
        var account = CreateAccount();

        // Act
        account.SetOpeningBalance(-194.91m, new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

        // Assert
        account.HasOpeningBalance.Should().BeTrue();
        account.OpeningBalance.Should().Be(-194.91m);
    }

    [Fact]
    public void ClearOpeningBalance_ShouldResetMarcoZero_WhenRecalculationIsNeeded()
    {
        // Arrange
        var account = CreateAccount();
        account.SetOpeningBalance(-194.91m, new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

        // Act
        account.ClearOpeningBalance();

        // Assert
        account.HasOpeningBalance.Should().BeFalse();
        account.OpeningBalance.Should().Be(0m);
        account.OpeningBalanceTransactionId.Should().BeNull();
    }

    #endregion
}
