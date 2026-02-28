using System.ComponentModel;
using System.Globalization;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram.Plugins;

/// <summary>
/// Plugin SK para gerenciamento de orçamentos mensais.
/// </summary>
public sealed class BudgetPlugin
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly BudgetService _budgetService;
    private readonly ILogger<BudgetPlugin> _logger;

    public BudgetPlugin(
        BudgetService budgetService,
        ILogger<BudgetPlugin> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    [KernelFunction("ListBudgets")]
    [Description("Lista os orçamentos do mês/ano informado com valor limite, gasto e porcentagem de uso.")]
    public async Task<string> ListBudgetsAsync(
        [Description("Mês (1-12)")] int month,
        [Description("Ano (ex: 2025)")] int year)
    {
        try
        {
            var budgets = await _budgetService.GetBudgetsByMonthAndYearAsync(month, year);
            var budgetList = budgets.ToList();

            if (budgetList.Count == 0)
                return $"📊 Nenhum orçamento definido para {month:D2}/{year}.";

            var lines = budgetList.Select(b =>
            {
                var status = b.UsagePercentage >= 90 ? "⚠️" : b.UsagePercentage >= 70 ? "🟡" : "✅";
                return $"• {b.Name}: R$ {b.UsedAmount.ToString("N2", PtBrCulture)} / R$ {b.LimitAmount.ToString("N2", PtBrCulture)} ({b.UsagePercentage:N0}%) {status}";
            });

            return $"📊 Orçamentos {month:D2}/{year}:\n{string.Join("\n", lines)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar orçamentos via Telegram SK");
            return $"❌ Erro ao listar orçamentos: {ex.Message}";
        }
    }

    [KernelFunction("CreateBudget")]
    [Description("Cria um novo orçamento mensal com limite de gastos.")]
    public async Task<string> CreateBudgetAsync(
        [Description("Nome do orçamento (ex: 'Alimentação', 'Lazer', 'Transporte')")] string name,
        [Description("Valor limite mensal")] decimal limitAmount,
        [Description("Mês (1-12)")] int month,
        [Description("Ano (ex: 2025)")] int year,
        [Description("Descrição opcional do orçamento")] string? description = null)
    {
        try
        {
            var request = new CreateBudgetRequest
            {
                Name = name,
                LimitAmount = limitAmount,
                Month = month,
                Year = year,
                Description = description
            };

            var created = await _budgetService.CreateBudgetAsync(request);
            return $"✅ Orçamento \"{created.Name}\" criado!\n• Limite: R$ {created.LimitAmount.ToString("N2", PtBrCulture)} para {month:D2}/{year}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar orçamento via Telegram SK");
            return $"❌ Erro ao criar o orçamento: {ex.Message}";
        }
    }

    [KernelFunction("GetBudgetUsage")]
    [Description("Consulta o uso detalhado de um orçamento específico pelo nome.")]
    public async Task<string> GetBudgetUsageAsync(
        [Description("Nome do orçamento")] string name,
        [Description("Mês (1-12)")] int month,
        [Description("Ano (ex: 2025)")] int year)
    {
        try
        {
            var budgets = await _budgetService.GetBudgetsByMonthAndYearAsync(month, year);
            var budget = budgets.FirstOrDefault(b =>
                b.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (budget == null)
                return $"❌ Orçamento \"{name}\" não encontrado para {month:D2}/{year}.";

            var status = budget.UsagePercentage >= 90 ? "⚠️ ATENÇÃO" : budget.UsagePercentage >= 70 ? "🟡 Cuidado" : "✅ Dentro do limite";

            return $"""
                📊 Orçamento: {budget.Name}
                • Limite: R$ {budget.LimitAmount.ToString("N2", PtBrCulture)}
                • Usado: R$ {budget.UsedAmount.ToString("N2", PtBrCulture)} ({budget.UsagePercentage:N0}%)
                • Restante: R$ {budget.RemainingAmount.ToString("N2", PtBrCulture)}
                • Status: {status}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar uso do orçamento via Telegram SK");
            return $"❌ Erro ao consultar o orçamento: {ex.Message}";
        }
    }
}
