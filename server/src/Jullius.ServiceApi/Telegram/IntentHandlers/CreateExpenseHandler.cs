using System.Globalization;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;

namespace Jullius.ServiceApi.Telegram.IntentHandlers;

public class CreateExpenseHandler : IIntentHandler
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private readonly FinancialTransactionService _transactionService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CreateExpenseHandler> _logger;

    private const string DefaultCategoryColor = "#607D8B";

    public IntentType HandledIntent => IntentType.CreateExpense;

    public CreateExpenseHandler(
        FinancialTransactionService transactionService,
        ICategoryRepository categoryRepository,
        ILogger<CreateExpenseHandler> logger)
    {
        _transactionService = transactionService;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public List<string> GetMissingFields(ConversationState state)
    {
        var missing = new List<string>();
        if (!state.HasData("description")) missing.Add("description");
        if (!state.HasData("amount")) missing.Add("amount");
        if (!state.HasData("categoryName")) missing.Add("categoryName");
        return missing;
    }

    public string BuildConfirmationMessage(ConversationState state)
    {
        var description = state.GetData<string>("description") ?? "N/A";
        var amount = state.GetData<decimal>("amount");
        var categoryName = state.GetData<string>("categoryName") ?? "N/A";
        var dueDate = state.GetData<DateTime?>("dueDate") ?? DateTime.UtcNow;
        var isPaid = state.GetData<bool>("isPaid");
        var paidText = isPaid ? "✅ Pago" : "⏳ Pendente";
        var amountText = amount.ToString("N2", PtBrCulture);

        return $"""
            📝 *Confirma o lançamento?*

            • Descrição: {description}
            • Valor: R$ {amountText}
            • Categoria: {categoryName}
            • Data: {dueDate:dd/MM/yyyy}
            • Status: {paidText}
            • Tipo: Despesa

            Responda *sim* para confirmar ou *não* para cancelar.
            """;
    }

    public async Task<string> HandleAsync(ConversationState state)
    {
        var missing = GetMissingFields(state);
        if (missing.Count > 0)
        {
            state.Phase = ConversationPhase.CollectingData;
            return await BuildMissingFieldQuestionAsync(missing.First(), state);
        }

        state.Phase = ConversationPhase.AwaitingConfirmation;
        return BuildConfirmationMessage(state);
    }

    public async Task<string> HandleConfirmationAsync(ConversationState state, bool confirmed)
    {
        if (!confirmed)
            return "❌ Lançamento cancelado.";

        try
        {
            var categoryName = state.GetData<string>("categoryName")!;
            var category = await _categoryRepository.GetByNameAsync(categoryName);

            if (category == null)
            {
                category = await _categoryRepository.GetOrCreateSystemCategoryAsync(categoryName, DefaultCategoryColor);
                _logger.LogInformation("Categoria criada automaticamente via Telegram: {Categoria}", categoryName);
            }

            var isPaid = state.GetData<bool>("isPaid");

            var request = new CreateFinancialTransactionRequest
            {
                Description = state.GetData<string>("description")!,
                Amount = state.GetData<decimal>("amount"),
                DueDate = state.GetData<DateTime?>("dueDate") ?? DateTime.UtcNow,
                Type = TransactionType.PayableBill,
                CategoryId = category.Id,
                IsPaid = isPaid,
                IsInstallment = false,
                InstallmentCount = 1
            };

            var transactions = await _transactionService.CreateTransactionAsync(request);
            var created = transactions.First();
            var paidLabel = isPaid ? " ✅" : "";

            return $"""
                ✅ Lançamento registrado com sucesso!
                • {created.Description} — R$ {created.Amount:N2} em {categoryName}{paidLabel}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar transação via Telegram");
            return $"❌ Erro ao registrar o lançamento: {ex.Message}";
        }
    }

    private async Task<string> BuildMissingFieldQuestionAsync(string field, ConversationState state)
    {
        if (field == "categoryName")
            return await FormatCategoryQuestionAsync(state);

        return field switch
        {
            "description" => "📝 Qual a descrição do gasto?",
            "amount" => "💰 Qual o valor?",
            _ => $"❓ Informe o campo: {field}"
        };
    }

    private async Task<string> FormatCategoryQuestionAsync(ConversationState state)
    {
        var description = state.GetData<string>("description") ?? "";
        var amount = state.GetData<decimal>("amount");
        var amountText = amount > 0 ? $" de R$ {amount.ToString("N2", PtBrCulture)}" : "";

        var categories = await _categoryRepository.GetAllAsync();
        var categoryList = categories.ToList();

        if (categoryList.Count > 0)
        {
            var categoryNames = string.Join(", ", categoryList.Select(c => c.Name));
            return $"📂 Entendi! {description}{amountText}.\nEm qual categoria devo lançar?\nSuas categorias: {categoryNames}";
        }

        return $"📂 Entendi! {description}{amountText}.\nEm qual categoria devo lançar? (ex: Alimentação, Saúde, Lazer)";
    }
}
