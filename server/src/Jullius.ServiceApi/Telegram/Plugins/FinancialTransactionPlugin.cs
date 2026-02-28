using System.ComponentModel;
using System.Globalization;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram.Plugins;

/// <summary>
/// Plugin SK para criação e consulta de transações financeiras (despesas e receitas).
/// Substitui CreateExpenseHandler e FinancialConsultingHandler.
/// </summary>
public sealed class FinancialTransactionPlugin
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    /// <summary>
    /// Paleta de cores vibrantes para categorias criadas automaticamente.
    /// A cor é selecionada deterministicamente com base no nome da categoria.
    /// </summary>
    private static readonly string[] CategoryColors =
    [
        "#4CAF50", // Verde
        "#2196F3", // Azul
        "#FF9800", // Laranja
        "#9C27B0", // Roxo
        "#F44336", // Vermelho
        "#00BCD4", // Ciano
        "#FF5722", // Laranja escuro
        "#3F51B5", // Índigo
        "#E91E63", // Rosa
        "#009688", // Teal
        "#FFC107", // Âmbar
        "#795548", // Marrom
    ];

    private readonly FinancialTransactionService _transactionService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly ILogger<FinancialTransactionPlugin> _logger;

    public FinancialTransactionPlugin(
        FinancialTransactionService transactionService,
        ICategoryRepository categoryRepository,
        IFinancialTransactionRepository transactionRepository,
        IBudgetRepository budgetRepository,
        ILogger<FinancialTransactionPlugin> logger)
    {
        _transactionService = transactionService;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
        _logger = logger;
    }

    [KernelFunction("CreateExpense")]
    [Description("Registra uma nova despesa (conta a pagar). Use quando o usuário informa um gasto realizado. IMPORTANTE: chame ListCategories ANTES e use uma categoria existente sempre que possível. Retorna confirmação com os dados registrados.")]
    public async Task<string> CreateExpenseAsync(
        [Description("Descrição do gasto, com primeira letra maiúscula (ex: 'Almoço', 'Conta de luz')")] string description,
        [Description("Valor numérico da despesa (ex: 45.90, 200, 2000)")] decimal amount,
        [Description("Nome EXATO de uma categoria existente (chame ListCategories antes). Só use um nome novo se nenhuma categoria existente for adequada.")] string categoryName,
        [Description("Se a despesa já foi paga. True para 'pago/paga/quitado', false caso contrário.")] bool isPaid = false,
        [Description("Data de vencimento no formato yyyy-MM-dd. Se não informada, usa a data atual.")] string? dueDate = null)
    {
        try
        {
            var category = await ResolveCategoryAsync(categoryName);

            var parsedDueDate = ParseDate(dueDate) ?? DateTime.UtcNow;

            var request = new CreateFinancialTransactionRequest
            {
                Description = description,
                Amount = amount,
                DueDate = parsedDueDate,
                Type = TransactionType.PayableBill,
                CategoryId = category.Id,
                IsPaid = isPaid,
                IsInstallment = false,
                InstallmentCount = 1
            };

            var transactions = await _transactionService.CreateTransactionAsync(request);
            var created = transactions.First();
            var paidLabel = isPaid ? " ✅" : "";

            return $"✅ Despesa registrada!\n• {created.Description} — R$ {created.Amount.ToString("N2", PtBrCulture)} em {categoryName}{paidLabel}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar despesa via Telegram SK");
            return $"❌ Erro ao registrar a despesa: {ex.Message}";
        }
    }

    [KernelFunction("CreateIncome")]
    [Description("Registra uma nova receita (conta a receber). Use quando o usuário informa um recebimento, salário, rendimento, entrada de dinheiro. IMPORTANTE: chame ListCategories ANTES e use uma categoria existente sempre que possível.")]
    public async Task<string> CreateIncomeAsync(
        [Description("Descrição da receita (ex: 'Salário', 'Freelance', 'Rendimento')")] string description,
        [Description("Valor numérico da receita")] decimal amount,
        [Description("Nome EXATO de uma categoria existente (chame ListCategories antes). Só use um nome novo se nenhuma categoria existente for adequada.")] string categoryName,
        [Description("Se a receita já foi recebida. True para 'recebido/recebida', false caso contrário.")] bool isPaid = false,
        [Description("Data de vencimento/recebimento no formato yyyy-MM-dd. Se não informada, usa a data atual.")] string? dueDate = null)
    {
        try
        {
            var category = await ResolveCategoryAsync(categoryName);

            var parsedDueDate = ParseDate(dueDate) ?? DateTime.UtcNow;

            var request = new CreateFinancialTransactionRequest
            {
                Description = description,
                Amount = amount,
                DueDate = parsedDueDate,
                Type = TransactionType.ReceivableBill,
                CategoryId = category.Id,
                IsPaid = isPaid,
                IsInstallment = false,
                InstallmentCount = 1
            };

            var transactions = await _transactionService.CreateTransactionAsync(request);
            var created = transactions.First();
            var receivedLabel = isPaid ? " ✅ Recebido" : " ⏳ Pendente";

            return $"✅ Receita registrada!\n• {created.Description} — R$ {created.Amount.ToString("N2", PtBrCulture)} em {categoryName}{receivedLabel}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar receita via Telegram SK");
            return $"❌ Erro ao registrar a receita: {ex.Message}";
        }
    }

    [KernelFunction("GetMonthlySummary")]
    [Description("Retorna o resumo financeiro do mês informado: receitas, despesas, saldo e status dos orçamentos. Use para responder perguntas sobre a situação financeira.")]
    public async Task<string> GetMonthlySummaryAsync(
        [Description("Mês (1-12). Use o mês atual se não especificado.")] int month,
        [Description("Ano (ex: 2025). Use o ano atual se não especificado.")] int year)
    {
        try
        {
            var transactions = await _transactionRepository.GetAllAsync();
            var monthlyTransactions = transactions
                .Where(t => t.DueDate.Month == month && t.DueDate.Year == year)
                .ToList();

            var budgets = await _budgetRepository.GetByMonthAndYearAsync(month, year);
            var budgetList = budgets.ToList();

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
                budgetInfo += $"\n- {budget.Name}: R$ {usedAmount.ToString("N2", PtBrCulture)} / R$ {budget.LimitAmount.ToString("N2", PtBrCulture)} ({percentage:N0}%) {status}";
            }

            return $"""
                Dados financeiros de {month:D2}/{year}:

                RECEITAS:
                - Total: R$ {totalIncome.ToString("N2", PtBrCulture)}
                - Recebido: R$ {receivedIncome.ToString("N2", PtBrCulture)}
                - Pendente: R$ {pendingIncome.ToString("N2", PtBrCulture)}

                DESPESAS:
                - Total: R$ {totalExpenses.ToString("N2", PtBrCulture)}
                - Pagas: R$ {paidExpenses.ToString("N2", PtBrCulture)}
                - Em aberto: R$ {openExpenses.ToString("N2", PtBrCulture)}

                SALDO:
                - Atual (realizado): R$ {actualBalance.ToString("N2", PtBrCulture)}
                - Projetado: R$ {projectedBalance.ToString("N2", PtBrCulture)}

                ORÇAMENTOS:{(budgetList.Count > 0 ? budgetInfo : "\n- Nenhum orçamento definido")}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar resumo financeiro mensal via Telegram SK");
            return $"❌ Erro ao buscar o resumo financeiro: {ex.Message}";
        }
    }

    [KernelFunction("UpdatePaymentStatus")]
    [Description("Marca uma transação financeira como paga ou pendente. Use quando o usuário diz que pagou algo ou quer reverter um pagamento.")]
    public async Task<string> UpdatePaymentStatusAsync(
        [Description("Descrição parcial da transação para busca (ex: 'conta de luz', 'almoço')")] string searchDescription,
        [Description("True para marcar como pago, false para marcar como pendente")] bool isPaid)
    {
        try
        {
            var allTransactions = await _transactionRepository.GetAllAsync();
            var normalizedSearch = searchDescription.Trim().ToLowerInvariant();

            var match = allTransactions
                .Where(t => t.Description.ToLowerInvariant().Contains(normalizedSearch))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();

            if (match == null)
                return $"❌ Não encontrei nenhuma transação com a descrição \"{searchDescription}\".";

            await _transactionService.UpdatePaymentStatusAsync(match.Id, isPaid);
            var statusText = isPaid ? "✅ Pago" : "⏳ Pendente";

            return $"{statusText}: {match.Description} — R$ {match.Amount.ToString("N2", PtBrCulture)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status de pagamento via Telegram SK");
            return $"❌ Erro ao atualizar o status: {ex.Message}";
        }
    }

    /// <summary>
    /// Resolve a categoria buscando por nome (case-insensitive). Se não encontrar correspondência exata,
    /// tenta correspondência parcial com categorias existentes. Cria nova categoria apenas como último recurso,
    /// atribuindo uma cor vibrante automaticamente.
    /// </summary>
    private async Task<Category> ResolveCategoryAsync(string categoryName)
    {
        // 1. Busca exata (case-insensitive — tratado no repositório)
        var exact = await _categoryRepository.GetByNameAsync(categoryName);
        if (exact != null)
            return exact;

        // 2. Correspondência parcial: "Não Planejado" → "Não planejado", etc.
        var all = await _categoryRepository.GetAllAsync();
        var normalized = categoryName.Trim().ToLowerInvariant();

        var partial = all.FirstOrDefault(c =>
            c.Name.ToLowerInvariant().Contains(normalized) ||
            normalized.Contains(c.Name.ToLowerInvariant()));

        if (partial != null)
        {
            _logger.LogInformation(
                "Categoria '{Solicitada}' resolvida para existente '{Existente}' via correspondência parcial",
                categoryName, partial.Name);
            return partial;
        }

        // 3. Nenhuma correspondência — criar com cor vibrante
        var color = GetColorForCategory(categoryName);
        _logger.LogInformation(
            "Categoria '{Categoria}' criada automaticamente via Telegram com cor {Cor}",
            categoryName, color);
        return await _categoryRepository.GetOrCreateSystemCategoryAsync(categoryName, color);
    }

    /// <summary>
    /// Gera uma cor da paleta de forma determinística baseada no nome da categoria.
    /// </summary>
    private static string GetColorForCategory(string categoryName)
    {
        var hash = categoryName.ToLowerInvariant().Aggregate(0, (h, c) => h * 31 + c);
        return CategoryColors[Math.Abs(hash) % CategoryColors.Length];
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return null;
    }
}
