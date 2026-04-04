using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jullius.ServiceApi.Application.Services;

public static partial class TextSearchNormalizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "as", "o", "os", "um", "uma", "uns", "umas",
        "de", "da", "das", "do", "dos",
        "em", "no", "na", "nos", "nas",
        "para", "por", "com", "sem", "e",
        "compra", "compras", "gasto", "gastos", "despesa", "despesas",
        "receita", "receitas", "conta", "contas", "pagamento", "pagamentos",
        "lancamento", "lancamentos", "registro", "registros"
    };

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return MultiSpaceRegex().Replace(builder.ToString().Normalize(NormalizationForm.FormC).Trim(), " ");
    }

    public static IReadOnlyCollection<string> Tokenize(string? text)
    {
        var normalized = Normalize(text);

        if (string.IsNullOrWhiteSpace(normalized))
            return Array.Empty<string>();

        return TokenSeparatorRegex()
            .Split(normalized)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Where(token => token.Length > 2 || token.All(char.IsDigit))
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static decimal CalculateSimilarity(string? source, string? target)
    {
        var normalizedSource = Normalize(source);
        var normalizedTarget = Normalize(target);

        if (string.IsNullOrWhiteSpace(normalizedSource) || string.IsNullOrWhiteSpace(normalizedTarget))
            return 0m;

        if (normalizedSource.Equals(normalizedTarget, StringComparison.Ordinal))
            return 1m;

        if (normalizedSource.Contains(normalizedTarget, StringComparison.Ordinal) ||
            normalizedTarget.Contains(normalizedSource, StringComparison.Ordinal))
            return 0.95m;

        var sourceTokens = Tokenize(normalizedSource);
        var targetTokens = Tokenize(normalizedTarget);

        if (sourceTokens.Count == 0 || targetTokens.Count == 0)
            return 0m;

        var overlap = sourceTokens.Intersect(targetTokens, StringComparer.Ordinal).ToArray();
        if (overlap.Length == 0)
            return 0m;

        var ratio = (decimal)overlap.Length / Math.Min(sourceTokens.Count, targetTokens.Count);
        var score = 0.35m + (ratio * 0.45m);

        if (overlap.Any(token => token.Length >= 5))
            score += 0.15m;

        return Math.Min(score, 0.95m);
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex("[^\\p{L}\\p{N}]+")]
    private static partial Regex TokenSeparatorRegex();
}
