using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

/// <summary>
/// Calcula o "Em Conta" do dashboard como saldo acumulado a partir do marco zero,
/// em vez do saldo isolado do mês. É o que permite bater com a soma real das contas.
///
/// O saldo de abertura de cada conta é lançado como FinancialTransaction ("Saldo anterior — conta"),
/// então o ledger é a única fonte da soma — somar OpeningBalance aqui contaria duas vezes.
/// </summary>
public class ConsolidatedBalanceService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;

    public ConsolidatedBalanceService(
        IBankAccountRepository bankAccountRepository,
        IFinancialTransactionRepository transactionRepository)
    {
        _bankAccountRepository = bankAccountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<ConsolidatedBalanceDto> GetBalanceAsync(int month, int year)
    {
        var accounts = (await _bankAccountRepository.GetActiveAsync()).ToList();
        var configured = accounts.Where(account => account.HasOpeningBalance).ToList();

        if (configured.Count == 0)
        {
            // Sem marco zero definido, o dashboard mantém a fórmula antiga.
            return new ConsolidatedBalanceDto { IsConfigured = false };
        }

        var openingDate = configured.Min(account => account.OpeningBalanceDate);
        var endOfMonth = EndOfMonthUtc(month, year);

        var result = new ConsolidatedBalanceDto
        {
            IsConfigured = true,
            OpeningBalanceDate = openingDate,
            SaldoBancos = configured.Sum(account => account.LastKnownBalance),
            SaldoBancosAtualizadoEm = configured
                .Where(account => account.LastBalanceSyncedAt.HasValue)
                .Select(account => account.LastBalanceSyncedAt!.Value)
                .DefaultIfEmpty()
                .Max(),
            Contas = configured
                .Select(account => new ConsolidatedAccountBalanceDto
                {
                    BankAccountId = account.Id,
                    Name = account.Name,
                    Institution = account.Institution,
                    LastKnownBalance = account.LastKnownBalance,
                    LastBalanceSyncedAt = account.LastBalanceSyncedAt
                })
                .ToList()
        };

        if (endOfMonth < openingDate)
        {
            result.IsHistoricalPeriod = true;
            return result;
        }

        result.EmConta = await _transactionRepository.GetRealizedNetAmountAsync(openingDate, endOfMonth);

        // A divergência só é comparável quando o período exibido já alcança o presente:
        // num mês passado o acumulado do ledger naturalmente difere do saldo atual do banco.
        if (endOfMonth >= DateTime.UtcNow.Date)
            result.Divergencia = result.EmConta - result.SaldoBancos;

        return result;
    }

    /// <summary>Saldo consolidado até agora, usado na conferência da tela de revisão.</summary>
    public async Task<ConsolidatedBalanceDto> GetCurrentBalanceAsync()
    {
        var today = DateTime.UtcNow;
        return await GetBalanceAsync(today.Month, today.Year);
    }

    private static DateTime EndOfMonthUtc(int month, int year)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateTime(year, month, lastDay, 23, 59, 59, DateTimeKind.Utc);
    }
}
