using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

/// <summary>
/// Encontra, no ledger, lançamentos que já representam uma linha do extrato — o gasto que
/// você registrou à mão ou pelo bot antes de o banco confirmar.
///
/// Existe porque valor igual não basta: no extrato real há "PIZZARIA DUOS R$ 195,00" no mesmo dia
/// de um "Investimento em limite de crédito Inter R$ 195,00", que são coisas distintas. Por isso o
/// ranqueamento pesa também a proximidade de data e a similaridade de descrição, e o vínculo
/// nunca é automático: a sugestão é oferecida, a decisão é do usuário.
/// </summary>
public sealed class TransactionMatchFinder
{
    /// <summary>Além disso, o lançamento manual costuma anteceder o pagamento em poucos dias.</summary>
    private static readonly TimeSpan MaxDateDistance = TimeSpan.FromDays(21);

    /// <summary>Abaixo disto o candidato não é sequer exibido.</summary>
    private const decimal MinimumScore = 0.45m;

    /// <summary>A partir daqui a tela oferece o vínculo em um clique.</summary>
    public const decimal SuggestionThreshold = 0.80m;

    private const int MaxCandidates = 6;

    public IReadOnlyList<TransactionMatch> Find(
        ReconciliationItem item,
        IEnumerable<FinancialTransaction> ledger,
        IReadOnlyDictionary<Guid, decimal> alreadyLinkedAmountByTransaction)
    {
        var ledgerDate = BankStatementNormalizer.ToLedgerDate(item.RawDate);
        var amount = item.AbsoluteAmount;

        return ledger
            .Where(transaction => transaction.Type == item.ProposedType)
            .Where(transaction => (transaction.DueDate - ledgerDate).Duration() <= MaxDateDistance)
            .Select(transaction => Build(transaction, item, ledgerDate, amount, alreadyLinkedAmountByTransaction))
            .Where(match => match.Score >= MinimumScore)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => (match.Transaction.DueDate - ledgerDate).Duration())
            .Take(MaxCandidates)
            .ToArray();
    }

    private static TransactionMatch Build(
        FinancialTransaction transaction,
        ReconciliationItem item,
        DateTime ledgerDate,
        decimal amount,
        IReadOnlyDictionary<Guid, decimal> alreadyLinked)
    {
        var reasons = new List<string>();

        // Quando outras linhas já apontam para este lançamento, o que interessa é a soma delas
        // (duas cobranças da Juvo no banco para um único lançamento de R$ 394,71, por exemplo).
        alreadyLinked.TryGetValue(transaction.Id, out var linkedSoFar);
        var combined = linkedSoFar + amount;

        var amountScore = ScoreAmount(transaction.Amount, amount, combined, linkedSoFar, reasons);
        var dateScore = ScoreDate(transaction.DueDate, ledgerDate, reasons);
        var textScore = TextSearchNormalizer.CalculateSimilarity(transaction.Description, item.ProposedDescription);

        if (textScore >= 0.70m)
            reasons.Add("descrição parecida");

        // A descrição precisa pesar o suficiente para que valor e data coincidentes, sozinhos,
        // não cheguem ao patamar de sugestão: no extrato real há uma pizzaria de R$ 195,00 no
        // mesmo dia de um investimento de R$ 195,00, e são coisas distintas.
        var score = (amountScore * 0.50m) + (dateScore * 0.20m) + (textScore * 0.30m);

        // Uma conta em aberto batendo ao centavo é a projeção esperando exatamente este pagamento
        // — sinal forte mesmo quando o banco descreve a cobrança com outras palavras
        // ("LIQUIDO DE VENCIMENTO" para o lançamento "Salário").
        if (!transaction.IsPaid)
        {
            reasons.Add("ainda não pago");

            if (amountScore == 1m)
            {
                score += 0.15m;
                reasons.Add("conta em aberto com valor exato");
            }
        }

        score = Math.Min(score, 1m);

        return new TransactionMatch(
            transaction,
            Math.Round(score, 4),
            reasons,
            linkedSoFar,
            combined,
            SuggestsUpdateAmount(transaction, amount, combined, linkedSoFar),
            transaction.DueDate.Date != ledgerDate.Date,
            !transaction.IsPaid);
    }

    private static decimal ScoreAmount(
        decimal transactionAmount,
        decimal itemAmount,
        decimal combined,
        decimal linkedSoFar,
        List<string> reasons)
    {
        if (transactionAmount == itemAmount)
        {
            reasons.Add("valor idêntico");
            return 1m;
        }

        if (linkedSoFar > 0m && transactionAmount == combined)
        {
            reasons.Add("soma com o item já vinculado fecha o valor");
            return 1m;
        }

        var reference = Math.Max(transactionAmount, itemAmount);
        if (reference == 0m)
            return 0m;

        var difference = Math.Abs(transactionAmount - itemAmount) / reference;

        if (difference <= 0.05m)
        {
            reasons.Add("valor próximo");
            return 0.85m;
        }

        if (difference <= 0.20m)
        {
            reasons.Add("valor aproximado");
            return 0.55m;
        }

        return difference <= 0.50m ? 0.25m : 0m;
    }

    private static decimal ScoreDate(DateTime transactionDate, DateTime ledgerDate, List<string> reasons)
    {
        var days = (transactionDate.Date - ledgerDate.Date).Duration().TotalDays;

        if (days == 0)
        {
            reasons.Add("mesma data");
            return 1m;
        }

        if (days <= 3)
        {
            reasons.Add($"{days:0} dia(s) de diferença");
            return 0.85m;
        }

        if (days <= 10)
        {
            reasons.Add($"{days:0} dias de diferença");
            return 0.55m;
        }

        return 0.25m;
    }

    private static bool SuggestsUpdateAmount(
        FinancialTransaction transaction,
        decimal itemAmount,
        decimal combined,
        decimal linkedSoFar)
    {
        // Só faz sentido corrigir quando o valor lançado difere do que o banco cobrou de fato.
        var target = linkedSoFar > 0m ? combined : itemAmount;
        return transaction.Amount != target;
    }
}

public sealed record TransactionMatch(
    FinancialTransaction Transaction,
    decimal Score,
    IReadOnlyList<string> Reasons,
    decimal AlreadyLinkedAmount,
    decimal CombinedAmount,
    bool SuggestUpdateAmount,
    bool SuggestUpdateDueDate,
    bool SuggestMarkAsPaid)
{
    public bool IsStrong => Score >= TransactionMatchFinder.SuggestionThreshold;
}
