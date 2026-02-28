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
}
