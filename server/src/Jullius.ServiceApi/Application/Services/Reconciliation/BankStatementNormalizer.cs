using System.Text.RegularExpressions;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

/// <summary>
/// Normalizações necessárias para transformar uma linha crua do extrato em lançamento do Jullius.
/// </summary>
public static partial class BankStatementNormalizer
{
    private const string BrazilIanaId = "America/Sao_Paulo";
    private const string BrazilWindowsId = "E. South America Standard Time";

    private static readonly TimeZoneInfo BrazilTimeZone = ResolveBrazilTimeZone();

    /// <summary>
    /// A Pluggy devolve timestamps mistos: umas transações vêm como meia-noite local convertida
    /// para UTC (03:00Z) e outras com o horário real do evento (05:55Z). O Jullius grava DueDate
    /// como meia-noite UTC do dia pretendido, então o dia tem que ser extraído no fuso de Brasília
    /// — senão uma compra da madrugada cai no dia seguinte.
    /// </summary>
    public static DateTime ToLedgerDate(DateTime pluggyDate)
    {
        var utc = pluggyDate.Kind switch
        {
            DateTimeKind.Utc => pluggyDate,
            DateTimeKind.Local => pluggyDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(pluggyDate, DateTimeKind.Utc)
        };

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, BrazilTimeZone);
        return new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Data de calendário escolhida pelo usuário (marco zero, início do sync). Diferente de
    /// <see cref="ToLedgerDate"/>: aqui NÃO se converte fuso, porque "01/08" é um dia do calendário
    /// e não um instante. Converter jogaria a meia-noite UTC de volta para o dia 31.
    /// </summary>
    public static DateTime ToCalendarDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    /// <summary>
    /// O Inter não devolve paymentData em nenhuma transação; a contraparte só existe embutida
    /// na descrição, no formato "PIX RECEBIDO - Cp :90400888-ERIEL MIQUILINO PEREIRA",
    /// onde o número é a raiz do CNPJ da instituição e o texto seguinte é o nome da contraparte.
    /// </summary>
    public static (string? InstitutionCnpjRoot, string? CounterpartyName) ExtractCounterparty(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return (null, null);

        var match = CounterpartyRegex().Match(description);
        if (!match.Success)
            return (null, null);

        var cnpjRoot = match.Groups[1].Value.Trim();
        var name = match.Groups[2].Value.Trim();

        return (
            string.IsNullOrWhiteSpace(cnpjRoot) ? null : cnpjRoot,
            string.IsNullOrWhiteSpace(name) ? null : name);
    }

    /// <summary>Mantém só dígitos, para comparar CPF/CNPJ vindos em formatos diferentes.</summary>
    public static string OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return NonDigitRegex().Replace(value, string.Empty);
    }

    /// <summary>
    /// Encurta a descrição crua para caber no limite de 200 caracteres de FinancialTransaction,
    /// colapsando os espaços múltiplos que os bancos usam para alinhar colunas.
    /// </summary>
    public static string ToLedgerDescription(string? rawDescription, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
            return "Lançamento sem descrição";

        var collapsed = MultiSpaceRegex().Replace(rawDescription.Trim(), " ");
        return collapsed.Length <= maxLength ? collapsed : collapsed[..maxLength].TrimEnd();
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        foreach (var id in new[] { BrazilIanaId, BrazilWindowsId })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // tenta o próximo identificador
            }
            catch (InvalidTimeZoneException)
            {
                // tenta o próximo identificador
            }
        }

        // Último recurso: horário de Brasília sem horário de verão (abolido no Brasil desde 2019).
        return TimeZoneInfo.CreateCustomTimeZone("Jullius.BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }

    [GeneratedRegex(@"Cp\s*:\s*(\d+)\s*-\s*(.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CounterpartyRegex();

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();
}
