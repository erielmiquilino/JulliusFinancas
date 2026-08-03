using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

/// <summary>
/// Conta bancária real do titular, espelhada do Open Finance (Pluggy).
/// Guarda o saldo de abertura (marco zero) que dá base ao cálculo consolidado do dashboard.
/// </summary>
public class BankAccount
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Institution { get; private set; }
    public string HolderName { get; private set; }

    /// <summary>
    /// Item da Pluggy. É o único sinal confiável de que a conexão está viva:
    /// contas de item excluído continuam respondendo 200 com dados congelados.
    /// </summary>
    public string PluggyItemId { get; private set; }
    public string PluggyAccountId { get; private set; }

    /// <summary>Saldo na véspera do marco zero. Pode ser negativo (cheque especial).</summary>
    public decimal OpeningBalance { get; private set; }
    public DateTime OpeningBalanceDate { get; private set; }
    public bool HasOpeningBalance { get; private set; }

    /// <summary>Lançamento "Saldo anterior" gerado no marco zero. Nulo quando a abertura era zero.</summary>
    public Guid? OpeningBalanceTransactionId { get; private set; }

    /// <summary>Último saldo lido da Pluggy, usado no indicador de divergência.</summary>
    public decimal LastKnownBalance { get; private set; }
    public DateTime? LastBalanceSyncedAt { get; private set; }

    /// <summary>Cursor da conciliação: o próximo sync começa a partir daqui.</summary>
    public DateTime? LastSyncedAt { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public BankAccount(
        string name,
        string institution,
        string holderName,
        string pluggyItemId,
        string pluggyAccountId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Institution = institution;
        HolderName = holderName;
        PluggyItemId = pluggyItemId;
        PluggyAccountId = pluggyAccountId;
        IsActive = true;
        HasOpeningBalance = false;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private BankAccount() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be empty");

        if (string.IsNullOrWhiteSpace(Institution))
            throw new ArgumentException("Institution cannot be empty");

        if (string.IsNullOrWhiteSpace(HolderName))
            throw new ArgumentException("HolderName cannot be empty");

        if (string.IsNullOrWhiteSpace(PluggyItemId))
            throw new ArgumentException("PluggyItemId cannot be empty");

        if (string.IsNullOrWhiteSpace(PluggyAccountId))
            throw new ArgumentException("PluggyAccountId cannot be empty");
    }

    public void UpdateDetails(string name, string institution, string holderName, string pluggyItemId, string pluggyAccountId)
    {
        Name = name;
        Institution = institution;
        HolderName = holderName;
        PluggyItemId = pluggyItemId;
        PluggyAccountId = pluggyAccountId;

        Validate();
    }

    /// <summary>
    /// Fixa o marco zero. O saldo de abertura é o saldo da conta na véspera de <paramref name="openingBalanceDate"/>,
    /// calculado como saldo atual menos o movimento ocorrido a partir dessa data.
    /// </summary>
    public void SetOpeningBalance(decimal openingBalance, DateTime openingBalanceDate, Guid? openingBalanceTransactionId)
    {
        OpeningBalance = openingBalance;
        OpeningBalanceDate = EnsureUtc(openingBalanceDate);
        OpeningBalanceTransactionId = openingBalanceTransactionId;
        HasOpeningBalance = true;
    }

    /// <summary>Desfaz o marco zero para permitir recalcular a abertura.</summary>
    public void ClearOpeningBalance()
    {
        OpeningBalance = 0m;
        OpeningBalanceDate = default;
        OpeningBalanceTransactionId = null;
        HasOpeningBalance = false;
    }

    public void RegisterBalance(decimal currentBalance)
    {
        LastKnownBalance = currentBalance;
        LastBalanceSyncedAt = DateTime.UtcNow;
    }

    public void RegisterSync(DateTime syncedUpTo)
    {
        LastSyncedAt = EnsureUtc(syncedUpTo);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
}
