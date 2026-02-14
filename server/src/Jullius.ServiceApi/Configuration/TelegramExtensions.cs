using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram;
using Jullius.ServiceApi.Telegram.IntentHandlers;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração dos serviços do Telegram e Gemini
/// </summary>
public static class TelegramExtensions
{
    /// <summary>
    /// Registra todos os serviços do subsistema Telegram + Gemini
    /// </summary>
    public static IServiceCollection AddTelegramServices(this IServiceCollection services)
    {
        // State management — singleton pois mantém estado em memória
        services.AddSingleton<ConversationStateStore>();

        // Gemini AI
        services.AddScoped<GeminiAssistantService>();

        // Intent handlers (Strategy pattern)
        services.AddScoped<IIntentHandler, CreateExpenseHandler>();
        services.AddScoped<IIntentHandler, CreateCardPurchaseHandler>();
        services.AddScoped<IIntentHandler, FinancialConsultingHandler>();

        // Orchestrator e Bot
        services.AddScoped<ConversationOrchestrator>();
        services.AddScoped<TelegramBotService>();

        // HttpClient para chamadas REST ao Gemini
        services.AddHttpClient("Gemini", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
