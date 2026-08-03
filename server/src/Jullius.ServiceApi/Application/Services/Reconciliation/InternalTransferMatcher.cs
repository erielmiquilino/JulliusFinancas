using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

/// <summary>
/// Identifica e pareia transferências entre contas do próprio titular (PIX de mesma titularidade).
/// Elas não são despesa nem receita: o dinheiro só mudou de bolso, e no consolidado precisam se anular.
///
/// A detecção é feita pelo NOME da contraparte, nunca pela instituição — o extrato mostra a mesma
/// instituição servindo tanto a contas próprias quanto a terceiros.
///
/// A anulação só acontece quando os DOIS lados estão presentes. Um lado sozinho vira
/// <see cref="ReconciliationReviewFlag.OrphanTransfer"/> e vai para revisão manual, porque anular
/// às cegas faria dinheiro real sumir do consolidado.
/// </summary>
public sealed class InternalTransferMatcher
{
    /// <summary>Folga de data entre as duas pernas: PIX é instantâneo, mas TED/agendamento pode atrasar.</summary>
    private static readonly TimeSpan PairingWindow = TimeSpan.FromDays(2);

    /// <summary>Categoria da Pluggy que sinaliza mesma titularidade (o Santander preenche; o Inter não).</summary>
    private const string SamePersonCategory = "Same person transfer";

    /// <summary>
    /// Nomes truncados pelo banco só são aceitos como prefixo a partir deste tamanho,
    /// para não casar "MARIA APARECIDA M" com outro titular por acidente.
    /// </summary>
    private const int MinimumPrefixLength = 12;

    public InternalTransferAnalysis Analyze(
        IReadOnlyList<ReconciliationItem> items,
        IReadOnlyCollection<string> holderNames,
        IReadOnlyCollection<string> holderDocuments)
    {
        var normalizedNames = holderNames
            .Select(TextSearchNormalizer.Normalize)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        var normalizedDocuments = holderDocuments
            .Select(BankStatementNormalizer.OnlyDigits)
            .Where(document => document.Length >= 11)
            .ToHashSet(StringComparer.Ordinal);

        var candidates = items
            .Where(item => IsOwnTransfer(item, normalizedNames, normalizedDocuments))
            .ToList();

        var pairs = new List<InternalTransferPair>();
        var matched = new HashSet<Guid>();

        foreach (var outflow in candidates.Where(item => item.RawAmount < 0).OrderBy(item => item.RawDate))
        {
            if (matched.Contains(outflow.Id))
                continue;

            var inflow = candidates.FirstOrDefault(candidate =>
                candidate.RawAmount > 0 &&
                !matched.Contains(candidate.Id) &&
                candidate.BankAccountId != outflow.BankAccountId &&
                candidate.AbsoluteAmount == outflow.AbsoluteAmount &&
                IsWithinPairingWindow(candidate.RawDate, outflow.RawDate));

            if (inflow is null)
                continue;

            matched.Add(outflow.Id);
            matched.Add(inflow.Id);
            pairs.Add(new InternalTransferPair(outflow, inflow));
        }

        var orphans = candidates
            .Where(item => !matched.Contains(item.Id))
            .ToList();

        return new InternalTransferAnalysis(pairs, orphans);
    }

    private static bool IsWithinPairingWindow(DateTime left, DateTime right)
    {
        return (left - right).Duration() <= PairingWindow;
    }

    private static bool IsOwnTransfer(
        ReconciliationItem item,
        IReadOnlySet<string> holderNames,
        IReadOnlySet<string> holderDocuments)
    {
        // 1. Documento da contraparte (só o Santander devolve paymentData).
        var document = BankStatementNormalizer.OnlyDigits(item.CounterpartyDocument);
        if (document.Length >= 11 && holderDocuments.Contains(document))
            return true;

        // 2. Nome da contraparte, vindo do paymentData ou extraído da descrição do Inter.
        if (MatchesHolderName(item.CounterpartyName, holderNames))
            return true;

        var (_, nameFromDescription) = BankStatementNormalizer.ExtractCounterparty(item.RawDescription);
        if (MatchesHolderName(nameFromDescription, holderNames))
            return true;

        // 3. O Santander não usa o marcador "Cp :": o nome vem solto no fim da descrição
        //    ("PIX ENVIADO   Eriel Miquilino Pereira"). Exige nome completo, então
        //    "MARIA APARECIDA MIQUILINO" não casa com outro titular.
        if (ContainsHolderName(item.RawDescription, holderNames))
            return true;

        // 4. Classificação da própria Pluggy, usada como último sinal.
        return string.Equals(item.RawCategory, SamePersonCategory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesHolderName(string? counterpartyName, IReadOnlySet<string> holderNames)
    {
        var normalized = TextSearchNormalizer.Normalize(counterpartyName);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (holderNames.Contains(normalized))
            return true;

        // Bancos truncam nomes longos na descrição do extrato; aceita prefixo, com tamanho mínimo
        // para não confundir titulares diferentes que compartilham as primeiras palavras.
        return normalized.Length >= MinimumPrefixLength &&
               holderNames.Any(holder => holder.StartsWith(normalized, StringComparison.Ordinal));
    }

    private static bool ContainsHolderName(string? description, IReadOnlySet<string> holderNames)
    {
        var normalized = TextSearchNormalizer.Normalize(description);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        return holderNames.Any(holder =>
            holder.Length >= MinimumPrefixLength &&
            normalized.Contains(holder, StringComparison.Ordinal));
    }
}

public sealed record InternalTransferPair(ReconciliationItem Outflow, ReconciliationItem Inflow);

public sealed record InternalTransferAnalysis(
    IReadOnlyList<InternalTransferPair> Pairs,
    IReadOnlyList<ReconciliationItem> Orphans);
