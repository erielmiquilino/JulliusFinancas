using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class ReconciliationSessionDto
{
    public Guid Id { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public ReconciliationSessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int TotalItems { get; set; }
    public int NeedsAttentionCount { get; set; }
    public int ReadyCount { get; set; }
    public int NettedCount { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount => TotalIncome - TotalExpenses;

    /// <summary>Saldo consolidado projetado caso a sessão seja confirmada como está.</summary>
    public decimal ProjectedBalance { get; set; }

    /// <summary>Saldo real somado das contas, para conferência antes de gravar.</summary>
    public decimal BankBalance { get; set; }

    public List<ReconciliationItemDto> Items { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ReconciliationItemDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public string BankAccountName { get; set; } = string.Empty;

    public string RawDescription { get; set; } = string.Empty;
    public decimal RawAmount { get; set; }
    public decimal AbsoluteAmount { get; set; }
    public DateTime RawDate { get; set; }
    public string? RawCategory { get; set; }
    public string? CounterpartyName { get; set; }
    public string? PaymentMethod { get; set; }

    public string ProposedDescription { get; set; } = string.Empty;
    public Guid? ProposedCategoryId { get; set; }
    public string? ProposedCategoryName { get; set; }
    public TransactionType ProposedType { get; set; }

    public ReconciliationItemStatus Status { get; set; }
    public ReconciliationReviewFlag ReviewFlag { get; set; }
    public Guid? MatchedItemId { get; set; }

    /// <summary>Explicação em português do motivo pelo qual a linha exige atenção.</summary>
    public string? ReviewReason { get; set; }
}

public class SyncReconciliationRequest
{
    /// <summary>
    /// Data inicial do primeiro sync. Nos sincronismos seguintes é ignorada:
    /// cada conta continua a partir do próprio LastSyncedAt.
    /// </summary>
    public DateTime? From { get; set; }
}

public class SyncReconciliationResultDto
{
    public Guid? SessionId { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int NettedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class UpdateReconciliationItemRequest
{
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }

    /// <summary>Approved, Ignored ou NettedInternal (anular manualmente uma transferência órfã).</summary>
    public ReconciliationItemStatus Status { get; set; }
}

public class ConfirmReconciliationResultDto
{
    public int PostedCount { get; set; }
    public int IgnoredCount { get; set; }
    public int NettedCount { get; set; }
    public decimal EmConta { get; set; }
    public decimal SaldoBancos { get; set; }
    public decimal Divergencia { get; set; }
}
