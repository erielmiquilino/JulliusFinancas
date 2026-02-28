using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram.Plugins;

/// <summary>
/// Plugin que fornece data/hora atual no fuso de Brasília.
/// Permite ao LLM resolver datas relativas como "amanhã" e "próxima segunda".
/// </summary>
public sealed class DateTimePlugin
{
    private static readonly TimeZoneInfo BrazilTimeZone = GetBrazilTimeZone();

    [KernelFunction("GetCurrentDateTime")]
    [Description("Retorna a data e hora atuais no fuso horário de Brasília (America/Sao_Paulo). Use para interpretar datas relativas como 'amanhã', 'próxima segunda-feira', 'dia 10'.")]
    public string GetCurrentDateTime()
    {
        var brazilNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone);
        return $"Data/hora atual (Brasília): {brazilNow:yyyy-MM-dd HH:mm}, {brazilNow:dddd}, dia da semana: {(int)brazilNow.DayOfWeek}";
    }

    [KernelFunction("GetCurrentDate")]
    [Description("Retorna apenas a data atual no fuso horário de Brasília no formato yyyy-MM-dd.")]
    public string GetCurrentDate()
    {
        var brazilNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone);
        return brazilNow.ToString("yyyy-MM-dd");
    }

    private static TimeZoneInfo GetBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
