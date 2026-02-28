using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram;

/// <summary>
/// Filtro de invocação de funções do Semantic Kernel.
/// Intercepta chamadas aos plugins para logging, tratamento de erros,
/// produção de respostas amigáveis em caso de falha e limitação de chamadas
/// por requisição para evitar loops infinitos de auto function calling.
/// </summary>
public sealed class FinancialFunctionFilter : IFunctionInvocationFilter
{
    /// <summary>
    /// Número máximo de invocações de função permitidas por requisição.
    /// Limita loops infinitos quando o modelo chama funções repetidamente.
    /// </summary>
    internal const int MaxInvocationsPerRequest = 8;

    /// <summary>
    /// Contador de invocações por requisição.
    /// O filtro é registrado como Scoped, logo cada requisição HTTP recebe uma instância limpa com contador em 0.
    /// </summary>
    private int _invocationCount;

    private readonly ILogger<FinancialFunctionFilter> _logger;

    public FinancialFunctionFilter(ILogger<FinancialFunctionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var functionName = $"{context.Function.PluginName}.{context.Function.Name}";
        _invocationCount++;

        if (_invocationCount > MaxInvocationsPerRequest)
        {
            _logger.LogWarning(
                "Limite de {Max} invocações excedido — bloqueando {FunctionName} (invocação #{Count})",
                MaxInvocationsPerRequest, functionName, _invocationCount);

            context.Result = new FunctionResult(
                context.Function,
                "⚠️ Limite de chamadas atingido. Use os dados já obtidos para responder ao usuário.");
            return;
        }

        _logger.LogInformation("SK Function chamada: {FunctionName} (#{Count})", functionName, _invocationCount);

        try
        {
            await next(context);

            _logger.LogInformation("SK Function concluída: {FunctionName}", functionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na SK Function {FunctionName}", functionName);

            // Retorna mensagem amigável preservando o fluxo do SK
            context.Result = new FunctionResult(
                context.Function,
                $"⚠️ Ocorreu um erro ao executar a operação. Tente novamente em breve.");
        }
    }
}
