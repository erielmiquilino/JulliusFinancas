using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Jullius.Tests.Mocks;
using Xunit;

namespace Jullius.Tests.Services;

public class ConsolidatedBalanceServiceTests
{
    private static readonly DateTime MarcoZero = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    private readonly RepositoryMocks _mocks;
    private readonly ConsolidatedBalanceService _service;

    public ConsolidatedBalanceServiceTests()
    {
        _mocks = new RepositoryMocks();
        _service = new ConsolidatedBalanceService(
            _mocks.BankAccountRepository.Object,
            _mocks.FinancialTransactionRepository.Object);
    }

    private static BankAccount CreateAccount(
        string name,
        decimal openingBalance,
        decimal currentBalance,
        bool withOpeningBalance = true)
    {
        var account = new BankAccount(name, name, "ERIEL MIQUILINO PEREIRA", "item-1", Guid.NewGuid().ToString());

        if (withOpeningBalance)
            account.SetOpeningBalance(openingBalance, MarcoZero, Guid.NewGuid());

        account.RegisterBalance(currentBalance);
        return account;
    }

    #region GetBalanceAsync Tests

    [Fact]
    public async Task GetBalanceAsync_ShouldReportNotConfigured_WhenNoAccountHasOpeningBalance()
    {
        // Arrange
        _mocks.SetupActiveBankAccounts([CreateAccount("Inter", 0m, 100m, withOpeningBalance: false)]);

        // Act
        var result = await _service.GetBalanceAsync(8, 2026);

        // Assert — sem marco zero o dashboard mantém a fórmula antiga.
        result.IsConfigured.Should().BeFalse();
        result.EmConta.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldMatchTheSumOfBankBalances_WhenLedgerIsReconciled()
    {
        // Arrange — cenário real de agosto/2026: Santander 6.196,97 e Inter -161,40.
        // Abertura total -194,91 mais o movimento conciliado de 6.230,48 fecha em 6.035,57.
        _mocks.SetupActiveBankAccounts(
        [
            CreateAccount("Santander", 0m, 6196.97m),
            CreateAccount("Inter", -194.91m, -161.40m)
        ]);
        _mocks.SetupRealizedNetAmount(6035.57m);

        // Act
        var result = await _service.GetBalanceAsync(8, 2026);

        // Assert
        result.IsConfigured.Should().BeTrue();
        result.EmConta.Should().Be(6035.57m);
        result.SaldoBancos.Should().Be(6035.57m);
        result.Divergencia.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldExposeDivergence_WhenLedgerDoesNotMatchBank()
    {
        // Arrange — um gasto em dinheiro lançado à mão descola o ledger do saldo bancário.
        _mocks.SetupActiveBankAccounts(
        [
            CreateAccount("Santander", 0m, 6196.97m),
            CreateAccount("Inter", -194.91m, -161.40m)
        ]);
        _mocks.SetupRealizedNetAmount(5935.57m);

        // Act
        var result = await _service.GetBalanceAsync(8, 2026);

        // Assert
        result.Divergencia.Should().Be(-100m);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldFlagHistoricalPeriod_WhenMonthIsBeforeOpeningBalance()
    {
        // Arrange — julho/2026 é anterior ao marco zero, onde o acumulado não faz sentido.
        _mocks.SetupActiveBankAccounts([CreateAccount("Inter", -194.91m, -161.40m)]);

        // Act
        var result = await _service.GetBalanceAsync(6, 2026);

        // Assert
        result.IsConfigured.Should().BeTrue();
        result.IsHistoricalPeriod.Should().BeTrue();
        result.EmConta.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldNotComputeDivergence_WhenPeriodIsInThePast()
    {
        // Arrange — o acumulado de um mês passado não é comparável com o saldo atual do banco.
        var past = DateTime.UtcNow.AddMonths(-2);
        _mocks.SetupActiveBankAccounts([CreateAccount("Inter", -194.91m, -161.40m)]);
        _mocks.SetupRealizedNetAmount(500m);

        // Act
        var result = await _service.GetBalanceAsync(past.Month, past.Year);

        // Assert
        result.Divergencia.Should().BeNull();
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldListNegativeAccounts_WhenOverdraftIsInUse()
    {
        // Arrange — cheque especial do Inter entra como saldo negativo mesmo.
        _mocks.SetupActiveBankAccounts(
        [
            CreateAccount("Santander", 0m, 6196.97m),
            CreateAccount("Inter", -194.91m, -161.40m)
        ]);
        _mocks.SetupRealizedNetAmount(6035.57m);

        // Act
        var result = await _service.GetBalanceAsync(8, 2026);

        // Assert
        result.Contas.Should().HaveCount(2);
        result.Contas.Should().ContainSingle(conta => conta.IsNegative)
            .Which.Name.Should().Be("Inter");
    }

    #endregion
}
