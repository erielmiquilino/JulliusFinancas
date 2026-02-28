using FluentAssertions;
using Jullius.ServiceApi.Telegram.Plugins;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testes para o DateTimePlugin que fornece data/hora ao LLM.
/// </summary>
public class DateTimePluginTests
{
    private readonly DateTimePlugin _plugin = new();

    [Fact]
    public void GetCurrentDateTime_ShouldReturnBrazilianTimezone()
    {
        var result = _plugin.GetCurrentDateTime();

        Assert.Contains("Data/hora atual (Brasília):", result);
        // Deve ter formato de data
        Assert.Matches(@"\d{4}-\d{2}-\d{2}", result);
        // Deve ter dia da semana em português
        Assert.Matches(@"(segunda|terça|quarta|quinta|sexta|sábado|domingo)", result);
    }

    [Fact]
    public void GetCurrentDate_ShouldReturnDateOnly()
    {
        var result = _plugin.GetCurrentDate();

        // Formato yyyy-MM-dd
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", result);
    }

    [Fact]
    public void GetCurrentDate_ShouldReturnTodayOrBrazilDate()
    {
        var result = _plugin.GetCurrentDate();
        var parsed = DateOnly.Parse(result);

        // Deve ser a data de hoje (margem de 1 dia para fuso)
        var diff = Math.Abs((parsed.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays);
        Assert.True(diff < 2, $"Data retornada ({result}) está muito distante de UTC agora");
    }
}
