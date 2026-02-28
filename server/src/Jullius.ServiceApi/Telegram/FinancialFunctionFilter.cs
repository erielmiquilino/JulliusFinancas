using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram;

/// <summary>
/// Filtro de invocação de funções do Semantic Kernel.
/// Intercepta chamadas aos plugins para logging, tratamento de erros
/// e produção de respostas amigáveis em caso de falha.
/// </summary>
public sealed class FinancialFunctionFilter : IFunctionInvocationFilter
{
    private readonly ILogger<FinancialFunctionFilter> _logger;

    public FinancialFunctionFilter(ILogger<FinancialFunctionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var functionName = $"{context.Function.PluginName}.{context.Function.Name}";
        _logger.LogInformation("SK Function chamada: {FunctionName}", functionName);

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
