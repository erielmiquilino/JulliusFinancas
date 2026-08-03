using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Integrations.Pluggy;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

public class BankAccountService
{
    /// <summary>Categoria de sistema dos lançamentos de abertura do marco zero.</summary>
    private const string OpeningBalanceCategoryName = "Saldo Anterior";
    private const string OpeningBalanceCategoryColor = "#607D8B";

    private readonly IBankAccountRepository _repository;
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly PluggyClient _pluggyClient;
    private readonly ILogger<BankAccountService> _logger;

    public BankAccountService(
        IBankAccountRepository repository,
        IFinancialTransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        PluggyClient pluggyClient,
        ILogger<BankAccountService> logger)
    {
        _repository = repository;
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _pluggyClient = pluggyClient;
        _logger = logger;
    }

    public async Task<IEnumerable<BankAccountDto>> GetAllAsync()
    {
        var accounts = await _repository.GetAllAsync();
        return accounts.Select(MapToDto);
    }

    public async Task<BankAccountDto?> GetByIdAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        return account is null ? null : MapToDto(account);
    }

    public async Task<BankAccountDto> CreateAsync(CreateBankAccountRequest request)
    {
        var existing = await _repository.GetByPluggyAccountIdAsync(request.PluggyAccountId);
        if (existing is not null)
            throw new ArgumentException("Já existe uma conta cadastrada com esse accountId da Pluggy.");

        var account = new BankAccount(
            request.Name,
            request.Institution,
            request.HolderName,
            request.PluggyItemId,
            request.PluggyAccountId);

        await _repository.CreateAsync(account);
        _logger.LogInformation("Conta bancária cadastrada. ContaId: {ContaId}, Instituição: {Instituicao}",
            account.Id, account.Institution);

        return MapToDto(account);
    }

    public async Task<BankAccountDto?> UpdateAsync(Guid id, UpdateBankAccountRequest request)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account is null)
            return null;

        account.UpdateDetails(
            request.Name,
            request.Institution,
            request.HolderName,
            request.PluggyItemId,
            request.PluggyAccountId);

        if (request.IsActive)
            account.Activate();
        else
            account.Deactivate();

        await _repository.UpdateAsync(account);
        return MapToDto(account);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account is null)
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    /// <summary>
    /// Lista as contas que a Pluggy expõe para um item, para o cadastro não depender
    /// de copiar accountId na mão. Cartões de crédito vêm sinalizados (fora de escopo nesta versão).
    /// </summary>
    public async Task<IEnumerable<DiscoveredAccountDto>> DiscoverAccountsAsync(string pluggyItemId)
    {
        await _pluggyClient.GetItemAsync(pluggyItemId);
        var accounts = await _pluggyClient.GetAccountsAsync(pluggyItemId);

        var discovered = new List<DiscoveredAccountDto>();
        foreach (var account in accounts)
        {
            var registered = await _repository.GetByPluggyAccountIdAsync(account.Id);
            discovered.Add(new DiscoveredAccountDto
            {
                PluggyAccountId = account.Id,
                Name = account.Name ?? "(sem nome)",
                Number = account.Number,
                Subtype = account.Subtype,
                Owner = account.Owner,
                Balance = account.Balance,
                IsCreditCard = account.IsCreditCard,
                AlreadyRegistered = registered is not null
            });
        }

        return discovered;
    }

    /// <summary>
    /// Fixa o marco zero da conta. O saldo de abertura é reconstruído como
    /// saldo atual menos o movimento a partir da data informada, e vira um lançamento
    /// "Saldo anterior — {conta}" para que o ledger seja a única fonte do consolidado.
    /// </summary>
    public async Task<BankAccountDto> SetOpeningBalanceAsync(Guid id, SetOpeningBalanceRequest request)
    {
        var account = await _repository.GetByIdAsync(id)
            ?? throw new ArgumentException("Conta bancária não encontrada");

        if (account.HasOpeningBalance)
            throw new ArgumentException(
                "Esta conta já tem saldo de abertura. Remova o marco zero antes de recalcular.");

        // Valida a conexão antes de qualquer leitura: contas de item excluído
        // continuam respondendo 200 com dados congelados.
        await _pluggyClient.GetItemAsync(account.PluggyItemId);

        var pluggyAccount = await _pluggyClient.GetAccountAsync(account.PluggyAccountId)
            ?? throw new ArgumentException("Conta não encontrada na Pluggy.");

        var openingDate = BankStatementNormalizer.ToCalendarDate(request.OpeningBalanceDate);
        var movementStart = openingDate.AddDays(1);
        var now = DateTime.UtcNow;

        var transactions = await _pluggyClient.GetTransactionsAsync(
            account.PluggyAccountId,
            movementStart,
            now);

        var movement = transactions.Sum(transaction => transaction.Amount);
        var openingBalance = pluggyAccount.Balance - movement;

        _logger.LogInformation(
            "Marco zero da conta {ContaId}: saldo atual {SaldoAtual}, movimento {Movimento}, abertura {Abertura}",
            account.Id, pluggyAccount.Balance, movement, openingBalance);

        Guid? transactionId = null;
        if (openingBalance != 0m)
        {
            var category = await _categoryRepository.GetOrCreateSystemCategoryAsync(
                OpeningBalanceCategoryName,
                OpeningBalanceCategoryColor);

            var transaction = new FinancialTransaction(
                description: $"Saldo anterior — {account.Name}",
                amount: Math.Abs(openingBalance),
                dueDate: openingDate,
                type: openingBalance < 0 ? TransactionType.PayableBill : TransactionType.ReceivableBill,
                categoryId: category.Id,
                isPaid: true);

            await _transactionRepository.CreateAsync(transaction);
            transactionId = transaction.Id;
        }

        account.SetOpeningBalance(openingBalance, openingDate, transactionId);
        account.RegisterBalance(pluggyAccount.Balance);

        // O sync seguinte parte do dia posterior ao marco zero: o movimento do próprio dia da
        // abertura já está embutido no saldo anterior e não pode ser lançado de novo.
        account.RegisterSync(movementStart);

        await _repository.UpdateAsync(account);
        return MapToDto(account);
    }

    /// <summary>Desfaz o marco zero e remove o lançamento de abertura gerado.</summary>
    public async Task<BankAccountDto?> ClearOpeningBalanceAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account is null)
            return null;

        if (account.OpeningBalanceTransactionId.HasValue)
            await _transactionRepository.DeleteAsync(account.OpeningBalanceTransactionId.Value);

        account.ClearOpeningBalance();
        await _repository.UpdateAsync(account);

        return MapToDto(account);
    }

    /// <summary>Verifica se cada conta ativa ainda tem conexão viva na Pluggy.</summary>
    public async Task<IEnumerable<BankAccountDto>> CheckConnectionsAsync()
    {
        var accounts = await _repository.GetAllAsync();
        var results = new List<BankAccountDto>();

        foreach (var account in accounts)
        {
            var dto = MapToDto(account);

            try
            {
                var item = await _pluggyClient.GetItemAsync(account.PluggyItemId);
                dto.IsConnectionAlive = true;
                dto.ConnectionMessage = $"Conectado ({item.Status ?? "sem status"}).";
            }
            catch (PluggyItemNotFoundException ex)
            {
                dto.IsConnectionAlive = false;
                dto.ConnectionMessage = ex.Message;
            }
            catch (Exception ex)
            {
                dto.IsConnectionAlive = null;
                dto.ConnectionMessage = $"Não foi possível verificar: {ex.Message}";
            }

            results.Add(dto);
        }

        return results;
    }

    private static BankAccountDto MapToDto(BankAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Institution = account.Institution,
        HolderName = account.HolderName,
        PluggyItemId = account.PluggyItemId,
        PluggyAccountId = account.PluggyAccountId,
        OpeningBalance = account.OpeningBalance,
        OpeningBalanceDate = account.HasOpeningBalance ? account.OpeningBalanceDate : null,
        HasOpeningBalance = account.HasOpeningBalance,
        LastKnownBalance = account.LastKnownBalance,
        LastBalanceSyncedAt = account.LastBalanceSyncedAt,
        LastSyncedAt = account.LastSyncedAt,
        IsActive = account.IsActive,
        CreatedAt = account.CreatedAt
    };
}
