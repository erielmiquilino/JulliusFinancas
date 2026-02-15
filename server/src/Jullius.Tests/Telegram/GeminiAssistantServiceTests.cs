using System.Reflection;
using FluentAssertions;
using Jullius.ServiceApi.Application.Services;
using Xunit;

namespace Jullius.Tests.Telegram;

public class GeminiAssistantServiceTests
{
    [Fact]
    public void BuildDateContextInstruction_ShouldUseBrazilTimezoneReference()
    {
        var method = typeof(GeminiAssistantService).GetMethod("BuildDateContextInstruction", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var utcNow = new DateTime(2026, 2, 16, 1, 30, 0, DateTimeKind.Utc);
        var instruction = method!.Invoke(null, [utcNow]) as string;

        instruction.Should().NotBeNull();
        instruction.Should().Contain("2026-02-15 22:30");
        instruction.Should().Contain("America/Sao_Paulo");
    }
}
