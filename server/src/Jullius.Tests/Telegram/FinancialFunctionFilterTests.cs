using FluentAssertions;
using Jullius.ServiceApi.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using System.ComponentModel;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testes para o FinancialFunctionFilter (IFunctionInvocationFilter).
/// Usa o Kernel real para invocar funções e verificar o comportamento do filtro.
/// </summary>
public class FinancialFunctionFilterTests
{
    private readonly FinancialFunctionFilter _filter;
    private readonly Mock<ILogger<FinancialFunctionFilter>> _loggerMock = new();

    public FinancialFunctionFilterTests()
    {
        _filter = new FinancialFunctionFilter(_loggerMock.Object);
    }

    [Fact]
    public void Filter_ShouldImplementIFunctionInvocationFilter()
    {
        Assert.IsAssignableFrom<IFunctionInvocationFilter>(_filter);
    }

    [Fact]
    public async Task Filter_ShouldAllowSuccessfulFunctionExecution()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter>(_filter);
        var kernel = builder.Build();

        var function = KernelFunctionFactory.CreateFromMethod(
            () => "Resultado OK",
            "TestFunction",
            "Função de teste");

        var result = await kernel.InvokeAsync(function);

        Assert.Equal("Resultado OK", result.GetValue<string>());
    }

    [Fact]
    public async Task Filter_ShouldReturnFriendlyError_WhenFunctionThrows()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter>(_filter);
        var kernel = builder.Build();

        var function = KernelFunctionFactory.CreateFromMethod(
            new Func<string>(() => throw new InvalidOperationException("Falha simulada")),
            "FailingFunction",
            "Função que falha");

        var result = await kernel.InvokeAsync(function);

        var value = result.GetValue<string>();
        Assert.Contains("erro", value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Filter_ShouldLogFunctionName()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter>(_filter);
        var kernel = builder.Build();

        var function = KernelFunctionFactory.CreateFromMethod(
            () => "ok",
            "MeuTeste",
            "Função de teste");

        await kernel.InvokeAsync(function);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MeuTeste")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Filter_ShouldBlockExecution_WhenInvocationLimitExceeded()
    {
        // Cada instância do filtro (Scoped) começa com contador em 0
        var freshFilter = new FinancialFunctionFilter(_loggerMock.Object);

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter>(freshFilter);
        var kernel = builder.Build();

        var callCount = 0;
        var function = KernelFunctionFactory.CreateFromMethod(
            () => { callCount++; return "ok"; },
            "RepeatableFunction",
            "Função de teste");

        // Executar 8 vezes (limite é 8) — todas devem funcionar
        for (var i = 0; i < FinancialFunctionFilter.MaxInvocationsPerRequest; i++)
            await kernel.InvokeAsync(function);

        callCount.Should().Be(FinancialFunctionFilter.MaxInvocationsPerRequest,
            "as primeiras invocações devem executar normalmente");

        // A 9ª invocação deve ser bloqueada
        var result = await kernel.InvokeAsync(function);
        var blocked = result.GetValue<string>();

        blocked.Should().Contain("Limite de chamadas atingido");
        callCount.Should().Be(FinancialFunctionFilter.MaxInvocationsPerRequest,
            "a invocação além do limite não deve executar a função real");
    }

    [Fact]
    public async Task Filter_NewInstance_ShouldStartWithFreshCounter()
    {
        // Simula o comportamento Scoped: nova instância = novo contador = aceita chamadas
        var filter1 = new FinancialFunctionFilter(_loggerMock.Object);
        var filter2 = new FinancialFunctionFilter(_loggerMock.Object);

        var builder1 = Kernel.CreateBuilder();
        builder1.Services.AddSingleton<IFunctionInvocationFilter>(filter1);
        var kernel1 = builder1.Build();

        var function = KernelFunctionFactory.CreateFromMethod(
            () => "ok",
            "TestFunction",
            "Função de teste");

        // Esgotar o limite na primeira instância
        for (var i = 0; i < FinancialFunctionFilter.MaxInvocationsPerRequest; i++)
            await kernel1.InvokeAsync(function);

        var blocked = await kernel1.InvokeAsync(function);
        blocked.GetValue<string>().Should().Contain("Limite");

        // Nova instância (simula nova requisição HTTP) deve funcionar normalmente
        var builder2 = Kernel.CreateBuilder();
        builder2.Services.AddSingleton<IFunctionInvocationFilter>(filter2);
        var kernel2 = builder2.Build();

        var fresh = await kernel2.InvokeAsync(function);
        fresh.GetValue<string>().Should().Be("ok");
    }
}
