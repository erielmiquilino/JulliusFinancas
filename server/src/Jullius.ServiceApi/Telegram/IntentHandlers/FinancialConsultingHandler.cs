using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;

namespace Jullius.ServiceApi.Telegram.IntentHandlers;

public class FinancialConsultingHandler : IIntentHandler
{
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly Application.Services.GeminiAssistantService _geminiService;
    private readonly ILogger<FinancialConsultingHandler> _logger;

    public IntentType HandledIntent => IntentType.FinancialConsulting;

    public FinancialConsultingHandler(
        IFinancialTransactionRepository transactionRepository,
        IBudgetRepository budgetRepository,
        Application.Services.GeminiAssistantService geminiService,
        ILogger<FinancialConsultingHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
        _geminiService = geminiService;
        _logger = logger;
    }

    public List<string> GetMissingFields(ConversationState state) => [];

    public string BuildConfirmationMessage(ConversationState state) => string.Empty;

    public async Task<string> HandleAsync(ConversationState state)
    {
        var question = state.GetData<string>("question") ?? "como estou esse mês?";

        try
        {
            var now = DateTime.UtcNow;
            var financialContext = await BuildFinancialContextAsync(now.Month, now.Year);

            var response = await _geminiService.GetFinancialAdviceAsync(question, financialContext);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar consultoria financeira via Telegram");
            return "❌ Desculpe, não consegui analisar seus dados financeiros no momento. Tente novamente.";
        }
    }

    public Task<string> HandleConfirmationAsync(ConversationState state, bool confirmed)
    {
        // Consulting never needs confirmation
        return Task.FromResult("Consulta encerrada.");
    }

    private async Task<string> BuildFinancialContextAsync(int month, int year)
    {
        var transactions = await _transactionRepository.GetAllAsync();
        var monthlyTransactions = transactions
            .Where(t => t.DueDate.Month == month && t.DueDate.Year == year)
            .ToList();

        var budgets = await _budgetRepository.GetByMonthAndYearAsync(month, year);
        var budgetList = budgets.ToList();

        // Calculate summary (same logic as dashboard frontend)
        var expenses = monthlyTransactions.Where(t => t.Type == TransactionType.PayableBill).ToList();
        var income = monthlyTransactions.Where(t => t.Type == TransactionType.ReceivableBill).ToList();

        var totalExpenses = expenses.Sum(t => t.Amount);
        var paidExpenses = expenses.Where(t => t.IsPaid).Sum(t => t.Amount);
        var openExpenses = totalExpenses - paidExpenses;

        var totalIncome = income.Sum(t => t.Amount);
        var receivedIncome = income.Where(t => t.IsPaid).Sum(t => t.Amount);
        var pendingIncome = totalIncome - receivedIncome;

        var actualBalance = receivedIncome - paidExpenses;
        var projectedBalance = totalIncome - totalExpenses;

        var budgetInfo = "";
        foreach (var budget in budgetList)
        {
            var usedAmount = monthlyTransactions
                .Where(t => t.BudgetId == budget.Id && t.Type == TransactionType.PayableBill)
                .Sum(t => t.Amount);
            var percentage = budget.LimitAmount > 0 ? (usedAmount / budget.LimitAmount * 100) : 0;
            var status = percentage >= 90 ? "⚠️" : percentage >= 70 ? "🟡" : "✅";
            budgetInfo += $"\n- {budget.Name}: R$ {usedAmount:N2} / R$ {budget.LimitAmount:N2} ({percentage:N0}%) {status}";
        }

        return $"""
            Dados financeiros de {month:D2}/{year}:

            RECEITAS:
            - Total: R$ {totalIncome:N2}
            - Recebido: R$ {receivedIncome:N2}
            - Pendente: R$ {pendingIncome:N2}

            DESPESAS:
            - Total: R$ {totalExpenses:N2}
            - Pagas: R$ {paidExpenses:N2}
            - Em aberto: R$ {openExpenses:N2}

            SALDO:
            - Atual (realizado): R$ {actualBalance:N2}
            - Projetado: R$ {projectedBalance:N2}

            ORÇAMENTOS:{(budgetList.Count > 0 ? budgetInfo : "\n- Nenhum orçamento definido")}
            """;
    }
}
