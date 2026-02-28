using Jullius.ServiceApi.Telegram;
using Jullius.ServiceApi.Telegram.Plugins;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração dos serviços do Semantic Kernel + Telegram.
/// Substitui o antigo TelegramExtensions.
/// </summary>
public static class SemanticKernelExtensions
{
    /// <summary>
    /// Registra todos os serviços do subsistema Telegram + Semantic Kernel
    /// </summary>
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        // HttpClient dedicado ao Gemini com timeout estendido.
        // Thinking models (Gemini 3 Flash) podem demorar >100s após function calling,
        // excedendo o default de 100s do HttpClient (.NET).
        services.AddHttpClient("GeminiApi", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Chat history — singleton pois mantém estado em memória entre requests
        services.AddSingleton<ChatHistoryStore>();

        // Plugins SK — scoped para injetar services/repositories do EF Core corretamente
        services.AddScoped<DateTimePlugin>();
        services.AddScoped<FinancialTransactionPlugin>();
        services.AddScoped<CardTransactionPlugin>();
        services.AddScoped<CategoryPlugin>();
        services.AddScoped<BudgetPlugin>();

        // Filtro de invocação de funções
        services.AddScoped<FinancialFunctionFilter>();

        // Orquestrador SK e Bot
        services.AddScoped<SemanticKernelOrchestrator>();
        services.AddScoped<TelegramBotService>();

        return services;
    }
}
