using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

/// <summary>
/// Uma rodada de conciliação: o que foi puxado do banco entre dois instantes,
/// aguardando revisão antes de virar lançamento.
/// </summary>
public class ReconciliationSession
{
    [Key]
    public Guid Id { get; private set; }
    public DateTime PeriodFrom { get; private set; }
    public DateTime PeriodTo { get; private set; }
    public ReconciliationSessionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public ReconciliationSession(DateTime periodFrom, DateTime periodTo)
    {
        Id = Guid.NewGuid();
        PeriodFrom = EnsureUtc(periodFrom);
        PeriodTo = EnsureUtc(periodTo);
        Status = ReconciliationSessionStatus.Draft;
        StartedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private ReconciliationSession() { }

    private void Validate()
    {
        if (PeriodTo < PeriodFrom)
            throw new ArgumentException("PeriodTo cannot be earlier than PeriodFrom");
    }

    public void Confirm()
    {
        if (Status != ReconciliationSessionStatus.Draft)
            throw new InvalidOperationException("Somente sessões em rascunho podem ser confirmadas");

        Status = ReconciliationSessionStatus.Confirmed;
        ClosedAt = DateTime.UtcNow;
    }

    public void Discard()
    {
        if (Status != ReconciliationSessionStatus.Draft)
            throw new InvalidOperationException("Somente sessões em rascunho podem ser descartadas");

        Status = ReconciliationSessionStatus.Discarded;
        ClosedAt = DateTime.UtcNow;
    }

    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
}

public enum ReconciliationSessionStatus
{
    Draft = 0,
    Confirmed = 1,
    Discarded = 2
}
