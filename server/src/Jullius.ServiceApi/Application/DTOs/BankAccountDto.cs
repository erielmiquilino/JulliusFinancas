namespace Jullius.ServiceApi.Application.DTOs;

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string PluggyItemId { get; set; } = string.Empty;
    public string PluggyAccountId { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime? OpeningBalanceDate { get; set; }
    public bool HasOpeningBalance { get; set; }
    public decimal LastKnownBalance { get; set; }
    public DateTime? LastBalanceSyncedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Falso quando o item já não existe na Pluggy e a conta precisa ser reconectada.</summary>
    public bool? IsConnectionAlive { get; set; }
    public string? ConnectionMessage { get; set; }
}

public class CreateBankAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string PluggyItemId { get; set; } = string.Empty;
    public string PluggyAccountId { get; set; } = string.Empty;
}

public class UpdateBankAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string PluggyItemId { get; set; } = string.Empty;
    public string PluggyAccountId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Fixa o marco zero. O saldo de abertura é calculado como
/// saldo atual na Pluggy menos o movimento ocorrido a partir de <see cref="OpeningBalanceDate"/>.
/// </summary>
public class SetOpeningBalanceRequest
{
    public DateTime OpeningBalanceDate { get; set; }
}

/// <summary>
/// Contas encontradas na Pluggy para um item, usadas para preencher o cadastro
/// sem o usuário precisar copiar accountId na mão.
/// </summary>
public class DiscoveredAccountDto
{
    public string PluggyAccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string? Subtype { get; set; }
    public string? Owner { get; set; }
    public decimal Balance { get; set; }
    public bool IsCreditCard { get; set; }
    public bool AlreadyRegistered { get; set; }
}
