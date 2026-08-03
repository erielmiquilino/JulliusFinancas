using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

/// <summary>
/// Uma linha do extrato aguardando revisão. O <see cref="ExternalId"/> (id da transação na Pluggy)
/// tem índice único e é a chave de idempotência: se já existe, a transação não é reimportada.
/// Itens marcados como <see cref="ReconciliationItemStatus.Ignored"/> permanecem na tabela
/// justamente para nunca mais reaparecerem.
/// </summary>
public class ReconciliationItem
{
    [Key]
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid BankAccountId { get; private set; }

    public string ExternalId { get; private set; }
    public string RawDescription { get; private set; }

    /// <summary>Valor com o sinal original da Pluggy: negativo é saída em conta corrente.</summary>
    public decimal RawAmount { get; private set; }
    public DateTime RawDate { get; private set; }
    public string? RawCategory { get; private set; }
    public string? CounterpartyName { get; private set; }
    public string? CounterpartyDocument { get; private set; }
    public string? PaymentMethod { get; private set; }

    public string ProposedDescription { get; private set; }
    public Guid? ProposedCategoryId { get; private set; }
    public TransactionType ProposedType { get; private set; }

    public ReconciliationItemStatus Status { get; private set; }
    public ReconciliationReviewFlag ReviewFlag { get; private set; }

    /// <summary>Contraparte da transferência interna, quando o par foi encontrado.</summary>
    public Guid? MatchedItemId { get; private set; }
    public Guid? CreatedTransactionId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public BankAccount BankAccount { get; private set; } = null!;
    public ReconciliationSession Session { get; private set; } = null!;

    public ReconciliationItem(
        Guid sessionId,
        Guid bankAccountId,
        string externalId,
        string rawDescription,
        decimal rawAmount,
        DateTime rawDate,
        string? rawCategory = null,
        string? counterpartyName = null,
        string? counterpartyDocument = null,
        string? paymentMethod = null)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        BankAccountId = bankAccountId;
        ExternalId = externalId;
        RawDescription = rawDescription;
        RawAmount = rawAmount;
        RawDate = EnsureUtc(rawDate);
        RawCategory = rawCategory;
        CounterpartyName = counterpartyName;
        CounterpartyDocument = counterpartyDocument;
        PaymentMethod = paymentMethod;

        ProposedDescription = rawDescription;
        ProposedType = rawAmount < 0 ? TransactionType.PayableBill : TransactionType.ReceivableBill;
        Status = ReconciliationItemStatus.Pending;
        ReviewFlag = ReconciliationReviewFlag.None;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private ReconciliationItem() { }

    private void Validate()
    {
        if (SessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty");

        if (BankAccountId == Guid.Empty)
            throw new ArgumentException("BankAccountId cannot be empty");

        if (string.IsNullOrWhiteSpace(ExternalId))
            throw new ArgumentException("ExternalId cannot be empty");

        if (string.IsNullOrWhiteSpace(RawDescription))
            throw new ArgumentException("RawDescription cannot be empty");

        if (RawAmount == 0)
            throw new ArgumentException("RawAmount cannot be zero");
    }

    /// <summary>Valor absoluto, que é o formato aceito por <see cref="FinancialTransaction"/>.</summary>
    public decimal AbsoluteAmount => Math.Abs(RawAmount);

    public void ApplyProposal(string description, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");

        ProposedDescription = description;
        ProposedCategoryId = categoryId;
    }

    public void Flag(ReconciliationReviewFlag flag) => ReviewFlag = flag;

    public void Approve()
    {
        if (Status == ReconciliationItemStatus.Posted)
            throw new InvalidOperationException("Item já lançado não pode ser aprovado novamente");

        Status = ReconciliationItemStatus.Approved;
    }

    public void Ignore()
    {
        if (Status == ReconciliationItemStatus.Posted)
            throw new InvalidOperationException("Item já lançado não pode ser ignorado");

        Status = ReconciliationItemStatus.Ignored;
    }

    public void MarkAsInternalTransfer(Guid? matchedItemId)
    {
        Status = ReconciliationItemStatus.NettedInternal;
        MatchedItemId = matchedItemId;
    }

    public void MarkAsPosted(Guid createdTransactionId)
    {
        Status = ReconciliationItemStatus.Posted;
        CreatedTransactionId = createdTransactionId;
    }

    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
}

public enum ReconciliationItemStatus
{
    Pending = 0,
    Approved = 1,
    Ignored = 2,
    NettedInternal = 3,
    Posted = 4
}

public enum ReconciliationReviewFlag
{
    None = 0,
    AmbiguousCategory = 1,
    OrphanTransfer = 2,
    PossibleDuplicate = 3
}
