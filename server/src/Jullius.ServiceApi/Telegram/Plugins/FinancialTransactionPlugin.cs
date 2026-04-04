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

    private readonly FinancialTransactionService _transactionService;
    private readonly CategoryResolutionService _categoryResolutionService;
    private readonly TransactionResolutionService _transactionResolutionService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly ILogger<FinancialTransactionPlugin> _logger;

    public FinancialTransactionPlugin(
        FinancialTransactionService transactionService,
        CategoryResolutionService categoryResolutionService,
        TransactionResolutionService transactionResolutionService,
        ICategoryRepository categoryRepository,
        IFinancialTransactionRepository transactionRepository,
        IBudgetRepository budgetRepository,
        ILogger<FinancialTransactionPlugin> logger)
    {
        _transactionService = transactionService;
        _categoryResolutionService = categoryResolutionService;
        _transactionResolutionService = transactionResolutionService;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
        _logger = logger;
    }

    [KernelFunction("CreateExpense")]
    [Description("Registra uma nova despesa. Se a categoria não for informada, tente inferir pelas categorias existentes e pelo histórico. Se não houver segurança, não registre e faça uma pergunta ao usuário.")]
    public async Task<string> CreateExpenseAsync(
        [Description("Descrição do gasto, com primeira letra maiúscula (ex: 'Almoço', 'Conta de luz')")] string description,
        [Description("Valor numérico da despesa (ex: 45.90, 200, 2000)")] decimal amount,
        [Description("Nome da categoria desejada. Deixe vazio quando o usuário não informar categoria.")] string? categoryName = null,
        [Description("Se a despesa já foi paga. O padrão deve ser true. Só use false quando o usuário disser explicitamente que está em aberto, pendente ou não paga.")] bool isPaid = true,
        [Description("Data de vencimento no formato yyyy-MM-dd. Se não informada, usa a data atual.")] string? dueDate = null)
    {
        return await CreateTransactionAsync(
            description,
            amount,
            categoryName,
            isPaid,
            dueDate,
            TransactionType.PayableBill);
    }

    [KernelFunction("CreateIncome")]
    [Description("Registra uma nova receita. Se a categoria não for informada, tente inferir pelas categorias existentes e pelo histórico. Se não houver segurança, não registre e faça uma pergunta ao usuário.")]
    public async Task<string> CreateIncomeAsync(
        [Description("Descrição da receita (ex: 'Salário', 'Freelance', 'Rendimento')")] string description,
        [Description("Valor numérico da receita")] decimal amount,
        [Description("Nome da categoria desejada. Deixe vazio quando o usuário não informar categoria.")] string? categoryName = null,
        [Description("Se a receita já foi recebida. O padrão deve ser true. Só use false quando o usuário disser explicitamente que ainda está pendente.")] bool isPaid = true,
        [Description("Data de vencimento/recebimento no formato yyyy-MM-dd. Se não informada, usa a data atual.")] string? dueDate = null)
    {
        return await CreateTransactionAsync(
            description,
            amount,
            categoryName,
            isPaid,
            dueDate,
            TransactionType.ReceivableBill);
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

    [KernelFunction("SearchTransactions")]
    [Description("Busca transações financeiras (contas a pagar/receber) por descrição em um mês/ano. Use para responder perguntas como 'quanto gastei com X', 'quais transações de Y'. Retorna cada transação individual com descrição, valor, status e categoria.")]
    public async Task<string> SearchTransactionsAsync(
        [Description("Termo de busca parcial na descrição (ex: 'myatã', 'aluguel', 'luz')")] string searchDescription,
        [Description("Mês (1-12). Use o mês atual se não especificado.")] int month,
        [Description("Ano (ex: 2026). Use o ano atual se não especificado.")] int year)
    {
        try
        {
            var allTransactions = await _transactionRepository.GetAllAsync();
            var normalizedSearch = TextSearchNormalizer.Normalize(searchDescription);

            var matches = allTransactions
                .Where(t => t.DueDate.Month == month && t.DueDate.Year == year)
                .Where(t => TextSearchNormalizer.Normalize(t.Description).Contains(normalizedSearch, StringComparison.Ordinal))
                .OrderBy(t => t.DueDate)
                .ToList();

            if (matches.Count == 0)
                return $"Nenhuma transação encontrada com \"{searchDescription}\" em {month:D2}/{year}.";

            var lines = matches.Select(t =>
            {
                var typeLabel = t.Type == TransactionType.PayableBill ? "Despesa" : "Receita";
                var statusLabel = t.IsPaid ? "✅ Pago" : "⏳ Pendente";
                var categoryName = t.Category?.Name ?? "—";
                return $"• {t.Description} — R$ {t.Amount.ToString("N2", PtBrCulture)} | {typeLabel} | {statusLabel} | {categoryName} | Venc: {t.DueDate:dd/MM/yyyy}";
            });

            var total = matches.Sum(t => t.Amount);
            var paidTotal = matches.Where(t => t.IsPaid).Sum(t => t.Amount);
            var pendingTotal = total - paidTotal;

            var header = $"Transações com \"{searchDescription}\" em {month:D2}/{year} ({matches.Count} encontradas):";
            var totals = $"Total: R$ {total.ToString("N2", PtBrCulture)} | Pago: R$ {paidTotal.ToString("N2", PtBrCulture)} | Pendente: R$ {pendingTotal.ToString("N2", PtBrCulture)}";
            return $"{header}\n\n{string.Join("\n", lines)}\n\n{totals}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transações por descrição via Telegram SK");
            return $"❌ Erro ao buscar transações: {ex.Message}";
        }
    }

    [KernelFunction("UpdatePaymentStatus")]
    [Description("Marca uma transação financeira como paga ou pendente. Sempre localize a transação com segurança. Se houver mais de uma compatível, não altere e pergunte ao usuário qual é.")]
    public async Task<string> UpdatePaymentStatusAsync(
        [Description("Descrição parcial da transação para busca (ex: 'conta de luz', 'almoço')")] string searchDescription,
        [Description("True para marcar como pago, false para marcar como pendente")] bool isPaid,
        [Description("Valor atual da transação, se o usuário informar. Use para desambiguar.")] decimal? currentAmount = null,
        [Description("Data atual da transação no formato yyyy-MM-dd, se o usuário informar. Use para desambiguar.")] string? currentDueDate = null)
    {
        try
        {
            if (!TryParseDate(currentDueDate, out var parsedCurrentDueDate))
                return "❌ A data informada para localizar a transação é inválida.";

            var matchResult = await _transactionResolutionService.ResolveAsync(searchDescription, currentAmount, parsedCurrentDueDate);
            if (matchResult.Status == TransactionMatchStatus.NotFound)
                return $"❌ Não encontrei nenhuma transação com a descrição \"{searchDescription}\".";

            if (matchResult.Status == TransactionMatchStatus.Ambiguous)
                return BuildTransactionAmbiguityQuestion(matchResult.Matches, "alterar o status");

            var match = matchResult.SingleMatch!;
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

    [KernelFunction("UpdateTransaction")]
    [Description("Atualiza uma transação financeira já lançada. Permite alterar descrição, valor, data, categoria, tipo e status. Se houver mais de uma transação compatível, não altere e pergunte ao usuário qual é.")]
    public async Task<string> UpdateTransactionAsync(
        [Description("Descrição atual da transação que deve ser localizada")] string searchDescription,
        [Description("Nova descrição. Deixe vazio para manter a atual.")] string? newDescription = null,
        [Description("Novo valor. Deixe vazio para manter o atual.")] decimal? newAmount = null,
        [Description("Nova categoria. Deixe vazio para manter a atual.")] string? newCategoryName = null,
        [Description("Novo status de pagamento. Deixe vazio para manter o atual.")] bool? isPaid = null,
        [Description("Nova data no formato yyyy-MM-dd. Deixe vazio para manter a atual.")] string? newDueDate = null,
        [Description("Novo tipo: 'expense' para despesa ou 'income' para receita. Deixe vazio para manter o atual.")] string? newType = null,
        [Description("Valor atual da transação, se o usuário informar. Use para desambiguar.")] decimal? currentAmount = null,
        [Description("Data atual da transação no formato yyyy-MM-dd, se o usuário informar. Use para desambiguar.")] string? currentDueDate = null)
    {
        try
        {
            if (!TryParseDate(currentDueDate, out var parsedCurrentDueDate))
                return "❌ A data atual informada para localizar a transação é inválida.";

            if (!TryParseDate(newDueDate, out var parsedNewDueDate))
                return "❌ A nova data informada para a transação é inválida.";

            var matchResult = await _transactionResolutionService.ResolveAsync(searchDescription, currentAmount, parsedCurrentDueDate);
            if (matchResult.Status == TransactionMatchStatus.NotFound)
                return $"❌ Não encontrei nenhuma transação com a descrição \"{searchDescription}\".";

            if (matchResult.Status == TransactionMatchStatus.Ambiguous)
                return BuildTransactionAmbiguityQuestion(matchResult.Matches, "alterar");

            var existingTransaction = matchResult.SingleMatch!;
            if (existingTransaction.CardId.HasValue)
                return "⚠️ Esse lançamento está vinculado a uma fatura de cartão. Para evitar inconsistências, eu não posso editar essa transação financeira por aqui.";

            var categoryResult = await ResolveUpdatedCategoryAsync(existingTransaction, newCategoryName, newDescription);
            if (!categoryResult.IsResolved)
                return BuildCategoryQuestion(existingTransaction.Description, categoryResult);

            if (!TryParseTransactionType(newType, existingTransaction.Type, out var resolvedType))
                return "❌ O tipo informado é inválido. Use 'expense' para despesa ou 'income' para receita.";

            var updateRequest = new UpdateFinancialTransactionRequest
            {
                Description = string.IsNullOrWhiteSpace(newDescription) ? existingTransaction.Description : newDescription.Trim(),
                Amount = newAmount ?? existingTransaction.Amount,
                DueDate = parsedNewDueDate ?? existingTransaction.DueDate,
                Type = resolvedType,
                CategoryId = categoryResult.Category!.Id,
                IsPaid = isPaid ?? existingTransaction.IsPaid,
                BudgetId = existingTransaction.BudgetId
            };

            var updatedTransaction = await _transactionService.UpdateTransactionAsync(existingTransaction.Id, updateRequest);
            if (updatedTransaction == null)
                return $"❌ Não consegui atualizar a transação \"{searchDescription}\".";

            return BuildUpdatedTransactionMessage(updatedTransaction, categoryResult.Category.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar transação via Telegram SK");
            return $"❌ Erro ao atualizar a transação: {ex.Message}";
        }
    }

    [KernelFunction("DeleteTransaction")]
    [Description("Exclui uma transação financeira já lançada. Sempre localize a transação com segurança. Se houver mais de uma compatível, não exclua e pergunte ao usuário qual é.")]
    public async Task<string> DeleteTransactionAsync(
        [Description("Descrição atual da transação que deve ser excluída")] string searchDescription,
        [Description("Valor atual da transação, se o usuário informar. Use para desambiguar.")] decimal? currentAmount = null,
        [Description("Data atual da transação no formato yyyy-MM-dd, se o usuário informar. Use para desambiguar.")] string? currentDueDate = null)
    {
        try
        {
            if (!TryParseDate(currentDueDate, out var parsedCurrentDueDate))
                return "❌ A data informada para localizar a transação é inválida.";

            var matchResult = await _transactionResolutionService.ResolveAsync(searchDescription, currentAmount, parsedCurrentDueDate);
            if (matchResult.Status == TransactionMatchStatus.NotFound)
                return $"❌ Não encontrei nenhuma transação com a descrição \"{searchDescription}\".";

            if (matchResult.Status == TransactionMatchStatus.Ambiguous)
                return BuildTransactionAmbiguityQuestion(matchResult.Matches, "excluir");

            var match = matchResult.SingleMatch!;
            if (match.CardId.HasValue)
                return "⚠️ Esse lançamento está vinculado a uma fatura de cartão. Para evitar inconsistências, eu não posso excluir essa transação financeira por aqui.";

            var deleted = await _transactionService.DeleteTransactionAsync(match.Id);
            if (!deleted)
                return $"❌ Não consegui excluir a transação \"{searchDescription}\".";

            return $"✅ Transação excluída: {match.Description} — R$ {match.Amount.ToString("N2", PtBrCulture)}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir transação via Telegram SK");
            return $"❌ Erro ao excluir a transação: {ex.Message}";
        }
    }

    private async Task<string> CreateTransactionAsync(
        string description,
        decimal amount,
        string? categoryName,
        bool isPaid,
        string? dueDate,
        TransactionType transactionType)
    {
        try
        {
            if (!TryParseDate(dueDate, out var parsedDueDate))
                return "❌ A data informada para o lançamento é inválida.";

            var categoryResult = await _categoryResolutionService.ResolveAsync(description, categoryName);
            if (!categoryResult.IsResolved)
                return BuildCategoryQuestion(description, categoryResult);

            var request = new CreateFinancialTransactionRequest
            {
                Description = description,
                Amount = amount,
                DueDate = parsedDueDate ?? DateTime.UtcNow,
                Type = transactionType,
                CategoryId = categoryResult.Category!.Id,
                IsPaid = isPaid,
                IsInstallment = false,
                InstallmentCount = 1
            };

            var transactions = await _transactionService.CreateTransactionAsync(request);
            var created = transactions.First();

            return BuildCreatedTransactionMessage(created, categoryResult.Category.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar transação via Telegram SK");
            return $"❌ Erro ao registrar a transação: {ex.Message}";
        }
    }

    private async Task<CategoryResolutionResult> ResolveUpdatedCategoryAsync(
        FinancialTransaction existingTransaction,
        string? newCategoryName,
        string? newDescription)
    {
        if (string.IsNullOrWhiteSpace(newCategoryName))
            return await ResolveExistingCategoryAsync(existingTransaction);

        var descriptionForResolution = string.IsNullOrWhiteSpace(newDescription)
            ? existingTransaction.Description
            : newDescription.Trim();

        return await _categoryResolutionService.ResolveAsync(descriptionForResolution, newCategoryName);
    }

    private async Task<CategoryResolutionResult> ResolveExistingCategoryAsync(FinancialTransaction transaction)
    {
        if (transaction.Category != null)
            return CategoryResolutionResult.Resolved(transaction.Category);

        var category = await _categoryRepository.GetByIdAsync(transaction.CategoryId);
        if (category != null)
            return CategoryResolutionResult.Resolved(category);

        return await _categoryResolutionService.ResolveAsync(transaction.Description, null);
    }

    private static string BuildCreatedTransactionMessage(FinancialTransaction transaction, string categoryName)
    {
        var typeLabel = transaction.Type == TransactionType.PayableBill ? "Despesa" : "Receita";
        var statusLabel = GetStatusLabel(transaction.Type, transaction.IsPaid);

        return $"✅ {typeLabel} registrada!\n• {transaction.Description} — R$ {transaction.Amount.ToString("N2", PtBrCulture)} | Categoria: {categoryName} | Status: {statusLabel}";
    }

    private static string BuildUpdatedTransactionMessage(FinancialTransaction transaction, string categoryName)
    {
        var typeLabel = transaction.Type == TransactionType.PayableBill ? "Despesa" : "Receita";
        var statusLabel = GetStatusLabel(transaction.Type, transaction.IsPaid);

        return $"✅ {typeLabel} atualizada!\n• {transaction.Description} — R$ {transaction.Amount.ToString("N2", PtBrCulture)} | Categoria: {categoryName} | Status: {statusLabel} | Venc: {transaction.DueDate:dd/MM/yyyy}";
    }

    private static string BuildCategoryQuestion(string description, CategoryResolutionResult categoryResult)
    {
        var suggestedCategories = categoryResult.SuggestedCategories.Count > 0
            ? string.Join(", ", categoryResult.SuggestedCategories.Select(category => category.Name))
            : null;

        if (!string.IsNullOrWhiteSpace(categoryResult.RequestedCategoryName))
        {
            var existingCategoriesText = string.IsNullOrWhiteSpace(suggestedCategories)
                ? null
                : $"uma existente, como {suggestedCategories}";

            if (string.IsNullOrWhiteSpace(existingCategoriesText))
                return $"Não encontrei a categoria \"{categoryResult.RequestedCategoryName}\" para \"{description}\". O que eu faço: cadastro \"{categoryResult.RequestedCategoryName}\"?";

            return $"Não encontrei a categoria \"{categoryResult.RequestedCategoryName}\" para \"{description}\". O que eu faço: cadastro \"{categoryResult.RequestedCategoryName}\" ou uso {existingCategoriesText}?";
        }

        if (!string.IsNullOrWhiteSpace(suggestedCategories))
            return $"Não consegui definir a categoria de forma segura para \"{description}\". O que eu faço: uso uma existente, como {suggestedCategories}, ou cadastro uma nova categoria para esse item?";

        return $"Não consegui definir a categoria de forma segura para \"{description}\". O que eu faço: cadastro uma nova categoria para esse item?";
    }

    private static string BuildTransactionAmbiguityQuestion(
        IEnumerable<FinancialTransaction> matches,
        string action)
    {
        var options = matches.Select(match =>
        {
            var categoryName = match.Category?.Name ?? "—";
            return $"• {match.Description} — R$ {match.Amount.ToString("N2", PtBrCulture)} | {categoryName} | {match.DueDate:dd/MM/yyyy}";
        });

        return $"Encontrei mais de uma transação parecida para {action}. Qual delas você quer usar?\n{string.Join("\n", options)}\n\nSe quiser, me diga o valor ou a data do registro correto.";
    }

    private static string GetStatusLabel(TransactionType transactionType, bool isPaid)
    {
        if (transactionType == TransactionType.PayableBill)
            return isPaid ? "✅ Pago" : "⏳ Em aberto";

        return isPaid ? "✅ Recebido" : "⏳ Pendente";
    }

    private static bool TryParseTransactionType(string? value, TransactionType fallback, out TransactionType result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = fallback;
            return true;
        }

        var normalized = TextSearchNormalizer.Normalize(value);
        if (normalized is "expense" or "despesa" or "payablebill")
        {
            result = TransactionType.PayableBill;
            return true;
        }

        if (normalized is "income" or "receita" or "receivablebill")
        {
            result = TransactionType.ReceivableBill;
            return true;
        }

        result = fallback;
        return false;
    }

    private static bool TryParseDate(string? dateStr, out DateTime? parsedDate)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            parsedDate = null;
            return true;
        }

        if (DateTime.TryParse(
                dateStr,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            parsedDate = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        parsedDate = null;
        return false;
    }
}
