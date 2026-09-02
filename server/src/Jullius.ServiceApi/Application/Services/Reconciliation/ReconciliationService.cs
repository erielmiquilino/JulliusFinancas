using System.Globalization;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Integrations.Pluggy;

namespace Jullius.ServiceApi.Application.Services.Reconciliation;

/// <summary>
/// Orquestra a conciliação: puxa o movimento do banco desde a última consulta, monta uma sessão
/// de revisão e, só depois da confirmação, grava os lançamentos.
/// </summary>
public class ReconciliationService
{
    /// <summary>O container roda sem cultura definida; sem isto o "C" imprime o símbolo genérico ¤.</summary>
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly IReconciliationRepository _repository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryResolutionService _categoryResolutionService;
    private readonly ConsolidatedBalanceService _balanceService;
    private readonly InternalTransferMatcher _transferMatcher;
    private readonly TransactionMatchFinder _matchFinder;
    private readonly PluggyClient _pluggyClient;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(
        IReconciliationRepository repository,
        IBankAccountRepository bankAccountRepository,
        IFinancialTransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        CategoryResolutionService categoryResolutionService,
        ConsolidatedBalanceService balanceService,
        InternalTransferMatcher transferMatcher,
        TransactionMatchFinder matchFinder,
        PluggyClient pluggyClient,
        ILogger<ReconciliationService> logger)
    {
        _repository = repository;
        _bankAccountRepository = bankAccountRepository;
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _categoryResolutionService = categoryResolutionService;
        _balanceService = balanceService;
        _transferMatcher = transferMatcher;
        _matchFinder = matchFinder;
        _pluggyClient = pluggyClient;
        _logger = logger;
    }

    public async Task<SyncReconciliationResultDto> SyncAsync(SyncReconciliationRequest request)
    {
        var openSession = await _repository.GetOpenSessionAsync();
        if (openSession is not null)
            throw new ArgumentException(
                "Já existe uma conciliação aguardando revisão. Confirme ou descarte antes de sincronizar de novo.");

        var accounts = (await _bankAccountRepository.GetActiveAsync()).ToList();
        if (accounts.Count == 0)
            throw new ArgumentException("Nenhuma conta bancária ativa cadastrada.");

        var now = DateTime.UtcNow;
        var warnings = new List<string>();
        var collected = new List<CollectedTransaction>();
        var skipped = 0;

        foreach (var account in accounts)
        {
            try
            {
                // O item é a única prova de que a conexão está viva: contas de item excluído
                // seguem respondendo 200 com dados congelados.
                await _pluggyClient.GetItemAsync(account.PluggyItemId);
            }
            catch (PluggyItemNotFoundException ex)
            {
                _logger.LogWarning("Conexão perdida na conta {ContaId}: {Mensagem}", account.Id, ex.Message);
                warnings.Add($"{account.Name}: {ex.Message}");
                continue;
            }

            var from = ResolveStartDate(account, request.From);
            var pluggyAccount = await _pluggyClient.GetAccountAsync(account.PluggyAccountId);
            if (pluggyAccount is null)
            {
                warnings.Add($"{account.Name}: conta não encontrada na Pluggy.");
                continue;
            }

            if (pluggyAccount.IsCreditCard)
            {
                warnings.Add($"{account.Name}: cartão de crédito ainda não é conciliado (previsto para a v2).");
                continue;
            }

            var transactions = await _pluggyClient.GetTransactionsAsync(account.PluggyAccountId, from, now);

            // Só entra o que já foi efetivado: PENDING ainda pode mudar de valor.
            var posted = transactions.Where(transaction => transaction.IsPosted).ToList();
            var pendingCount = transactions.Count - posted.Count;
            if (pendingCount > 0)
                warnings.Add($"{account.Name}: {pendingCount} lançamento(s) ainda pendente(s) no banco, não importados.");

            collected.AddRange(posted.Select(transaction => new CollectedTransaction(account, transaction)));

            account.RegisterBalance(pluggyAccount.Balance);
            await _bankAccountRepository.UpdateAsync(account);
        }

        if (collected.Count == 0)
        {
            return new SyncReconciliationResultDto
            {
                Warnings = warnings,
                Message = "Nenhum lançamento novo encontrado no período."
            };
        }

        // Idempotência: o id da transação na Pluggy nunca é importado duas vezes,
        // inclusive quando foi ignorado numa conciliação anterior.
        var knownIds = await _repository.GetKnownExternalIdsAsync(collected.Select(item => item.Transaction.Id));
        var fresh = collected.Where(item => !knownIds.Contains(item.Transaction.Id)).ToList();
        skipped = collected.Count - fresh.Count;

        if (fresh.Count == 0)
        {
            return new SyncReconciliationResultDto
            {
                SkippedCount = skipped,
                Warnings = warnings,
                Message = "Nada novo: todos os lançamentos do período já haviam sido conciliados."
            };
        }

        var periodFrom = fresh.Min(item => item.Transaction.Date);
        var session = await _repository.CreateSessionAsync(new ReconciliationSession(periodFrom, now));

        var items = fresh.Select(item => BuildItem(session.Id, item)).ToList();

        var analysis = ApplyInternalTransferRules(items, accounts);
        await ApplyCategoryProposalsAsync(items);
        await FlagPossibleDuplicatesAsync(items);

        await _repository.AddItemsAsync(items);

        _logger.LogInformation(
            "Conciliação criada. SessaoId: {SessaoId}, importados: {Importados}, ignorados por duplicidade: {Pulados}, anulados: {Anulados}",
            session.Id, items.Count, skipped, analysis.Pairs.Count * 2);

        return new SyncReconciliationResultDto
        {
            SessionId = session.Id,
            ImportedCount = items.Count,
            SkippedCount = skipped,
            NettedCount = analysis.Pairs.Count * 2,
            Warnings = warnings,
            Message = $"{items.Count} lançamento(s) prontos para revisão."
        };
    }

    public async Task<ReconciliationSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session is null)
            return null;

        var items = (await _repository.GetItemsBySessionAsync(sessionId)).ToList();
        return await BuildSessionDtoAsync(session, items);
    }

    public async Task<ReconciliationSessionDto?> GetOpenSessionAsync()
    {
        var session = await _repository.GetOpenSessionAsync();
        if (session is null)
            return null;

        var items = (await _repository.GetItemsBySessionAsync(session.Id)).ToList();
        return await BuildSessionDtoAsync(session, items);
    }

    public async Task<ReconciliationItemDto?> UpdateItemAsync(Guid itemId, UpdateReconciliationItemRequest request)
    {
        var item = await _repository.GetItemByIdAsync(itemId);
        if (item is null)
            return null;

        if (item.Status == ReconciliationItemStatus.Posted)
            throw new ArgumentException("Este lançamento já foi confirmado e não pode mais ser alterado.");

        item.ApplyProposal(
            BankStatementNormalizer.ToLedgerDescription(request.Description),
            request.CategoryId);

        switch (request.Status)
        {
            case ReconciliationItemStatus.Approved:
                if (request.CategoryId is null)
                    throw new ArgumentException("Escolha uma categoria antes de aprovar o lançamento.");
                item.Approve();
                item.Flag(ReconciliationReviewFlag.None);
                break;

            case ReconciliationItemStatus.Ignored:
                item.Ignore();
                item.Flag(ReconciliationReviewFlag.None);
                break;

            case ReconciliationItemStatus.NettedInternal:
                item.MarkAsInternalTransfer(item.MatchedItemId);
                item.Flag(ReconciliationReviewFlag.None);
                break;

            case ReconciliationItemStatus.Pending:
                break;

            default:
                throw new ArgumentException("Status inválido para revisão.");
        }

        await _repository.UpdateItemAsync(item);

        return await BuildItemDtoAsync(item);
    }

    /// <summary>
    /// Lançamentos do ledger que podem ser o mesmo evento desta linha do extrato.
    /// </summary>
    public async Task<IEnumerable<MatchCandidateDto>> GetMatchCandidatesAsync(Guid itemId, string? search)
    {
        var item = await _repository.GetItemByIdAsync(itemId)
            ?? throw new ArgumentException("Item de conciliação não encontrado.");

        var ledgerDate = BankStatementNormalizer.ToLedgerDate(item.RawDate);
        var ledger = (await _transactionRepository.GetByDueDateRangeAsync(
            ledgerDate.AddDays(-40), ledgerDate.AddDays(40))).ToList();

        var linkedAmounts = await GetLinkedAmountsAsync(item.SessionId, exceptItemId: itemId);
        var categories = (await _categoryRepository.GetAllAsync()).ToList();

        // Busca livre permite alcançar um lançamento fora da janela de sugestão.
        if (!string.IsNullOrWhiteSpace(search))
        {
            return ledger
                .Where(t => TextSearchNormalizer.Normalize(t.Description)
                    .Contains(TextSearchNormalizer.Normalize(search), StringComparison.Ordinal))
                .OrderBy(t => (t.DueDate - ledgerDate).Duration())
                .Take(20)
                .Select(t => MapCandidate(new TransactionMatch(
                    t, 0m, Array.Empty<string>(),
                    linkedAmounts.GetValueOrDefault(t.Id),
                    linkedAmounts.GetValueOrDefault(t.Id) + item.AbsoluteAmount,
                    t.Amount != item.AbsoluteAmount,
                    t.DueDate.Date != ledgerDate.Date,
                    !t.IsPaid), categories))
                .ToArray();
        }

        return _matchFinder.Find(item, ledger, linkedAmounts)
            .Select(match => MapCandidate(match, categories))
            .ToArray();
    }

    public async Task<ReconciliationItemDto?> LinkItemAsync(Guid itemId, LinkReconciliationItemRequest request)
    {
        var item = await _repository.GetItemByIdAsync(itemId);
        if (item is null)
            return null;

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId)
            ?? throw new ArgumentException("O lançamento informado não existe.");

        if (transaction.Type != item.ProposedType)
            throw new ArgumentException(
                "O lançamento é de tipo diferente (entrada x saída) e não pode ser vinculado a esta linha.");

        item.LinkTo(request.TransactionId, request.UpdateAmount, request.UpdateDueDate, request.MarkAsPaid);
        await _repository.UpdateItemAsync(item);

        _logger.LogInformation(
            "Item vinculado a lançamento existente. ItemId: {ItemId}, LancamentoId: {LancamentoId}, corrige valor: {CorrigeValor}",
            itemId, request.TransactionId, request.UpdateAmount);

        return await BuildItemDtoAsync(item);
    }

    public async Task<ReconciliationItemDto?> UnlinkItemAsync(Guid itemId)
    {
        var item = await _repository.GetItemByIdAsync(itemId);
        if (item is null)
            return null;

        item.Unlink();
        await _repository.UpdateItemAsync(item);

        return await BuildItemDtoAsync(item);
    }

    /// <summary>Soma, por lançamento, das linhas já vinculadas — base do caso N:1.</summary>
    private async Task<Dictionary<Guid, decimal>> GetLinkedAmountsAsync(Guid sessionId, Guid? exceptItemId = null)
    {
        var items = await _repository.GetItemsBySessionAsync(sessionId);

        return items
            .Where(i => i.LinkedTransactionId.HasValue)
            .Where(i => exceptItemId is null || i.Id != exceptItemId)
            .GroupBy(i => i.LinkedTransactionId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.AbsoluteAmount));
    }

    private static MatchCandidateDto MapCandidate(TransactionMatch match, List<Category> categories) => new()
    {
        TransactionId = match.Transaction.Id,
        Description = match.Transaction.Description,
        Amount = match.Transaction.Amount,
        DueDate = match.Transaction.DueDate,
        IsPaid = match.Transaction.IsPaid,
        CategoryName = match.Transaction.Category?.Name
            ?? categories.FirstOrDefault(c => c.Id == match.Transaction.CategoryId)?.Name,
        Score = match.Score,
        Reasons = match.Reasons.ToList(),
        AlreadyLinkedAmount = match.AlreadyLinkedAmount,
        CombinedAmount = match.CombinedAmount,
        SuggestUpdateAmount = match.SuggestUpdateAmount,
        SuggestUpdateDueDate = match.SuggestUpdateDueDate,
        SuggestMarkAsPaid = match.SuggestMarkAsPaid
    };

    private async Task<ReconciliationItemDto> BuildItemDtoAsync(ReconciliationItem item)
    {
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        var referenced = await LoadReferencedTransactionsAsync(new[] { item });
        return MapItemToDto(item, categories, referenced);
    }

    /// <summary>Carrega os lançamentos citados por vínculo ou sugestão, para a tela mostrar o nome deles.</summary>
    private async Task<Dictionary<Guid, FinancialTransaction>> LoadReferencedTransactionsAsync(
        IEnumerable<ReconciliationItem> items)
    {
        var ids = items
            .SelectMany(i => new[] { i.LinkedTransactionId, i.SuggestedTransactionId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var map = new Dictionary<Guid, FinancialTransaction>();
        foreach (var id in ids)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction is not null)
                map[id] = transaction;
        }

        return map;
    }

    /// <summary>
    /// Grava os lançamentos aprovados. Itens ainda pendentes que já tenham categoria resolvida
    /// entram junto — é o comportamento de pré-aprovação da tela de revisão.
    /// </summary>
    public async Task<ConfirmReconciliationResultDto> ConfirmAsync(Guid sessionId)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId)
            ?? throw new ArgumentException("Conciliação não encontrada.");

        if (session.Status != ReconciliationSessionStatus.Draft)
            throw new ArgumentException("Esta conciliação já foi encerrada.");

        var items = (await _repository.GetItemsBySessionAsync(sessionId)).ToList();

        // Vinculados não precisam de categoria: quem manda é o lançamento existente.
        var blocked = items
            .Where(item => item.Status == ReconciliationItemStatus.Pending && item.ProposedCategoryId is null)
            .ToList();

        if (blocked.Count > 0)
            throw new ArgumentException(
                $"{blocked.Count} lançamento(s) ainda estão sem categoria. Revise ou ignore antes de confirmar.");

        // Itens vinculados corrigem um lançamento que já existe, em vez de criar outro.
        // Quando várias linhas do banco apontam para o mesmo lançamento (duas cobranças da Juvo
        // para uma única parcela, por exemplo), o valor correto é a soma delas.
        var linked = items.Where(item => item.Status == ReconciliationItemStatus.Linked).ToList();

        foreach (var group in linked.GroupBy(item => item.LinkedTransactionId!.Value))
        {
            var transaction = await _transactionRepository.GetByIdAsync(group.Key);
            if (transaction is null)
            {
                _logger.LogWarning(
                    "Lançamento vinculado não existe mais e foi ignorado. LancamentoId: {LancamentoId}", group.Key);
                continue;
            }

            var amount = group.Any(item => item.LinkUpdateAmount)
                ? group.Sum(item => item.AbsoluteAmount)
                : transaction.Amount;

            var dueDate = group.Any(item => item.LinkUpdateDueDate)
                ? BankStatementNormalizer.ToLedgerDate(group.Min(item => item.RawDate))
                : transaction.DueDate;

            var isPaid = transaction.IsPaid || group.Any(item => item.LinkMarkAsPaid);

            transaction.UpdateDetails(
                transaction.Description,
                amount,
                dueDate,
                transaction.Type,
                transaction.CategoryId,
                isPaid,
                budgetId: transaction.BudgetId);

            await _transactionRepository.UpdateAsync(transaction);

            foreach (var item in group)
                item.MarkAsPosted(transaction.Id);

            _logger.LogInformation(
                "Lançamento existente atualizado pela conciliação. LancamentoId: {LancamentoId}, valor: {Valor}, pago: {Pago}, linhas: {Linhas}",
                transaction.Id, amount, isPaid, group.Count());
        }

        var toPost = items
            .Where(item => item.Status is ReconciliationItemStatus.Approved or ReconciliationItemStatus.Pending)
            .ToList();

        foreach (var item in toPost)
        {
            var transaction = new FinancialTransaction(
                description: item.ProposedDescription,
                amount: item.AbsoluteAmount,
                dueDate: BankStatementNormalizer.ToLedgerDate(item.RawDate),
                type: item.ProposedType,
                categoryId: item.ProposedCategoryId!.Value,
                isPaid: true);

            await _transactionRepository.CreateAsync(transaction);
            item.MarkAsPosted(transaction.Id);
        }

        await _repository.UpdateItemsAsync(items);

        // Cada conta avança o cursor até o fim do período conciliado.
        foreach (var accountId in items.Select(item => item.BankAccountId).Distinct())
        {
            var account = await _bankAccountRepository.GetByIdAsync(accountId);
            if (account is null)
                continue;

            account.RegisterSync(session.PeriodTo);
            await _bankAccountRepository.UpdateAsync(account);
        }

        session.Confirm();
        await _repository.UpdateSessionAsync(session);

        var balance = await _balanceService.GetCurrentBalanceAsync();

        _logger.LogInformation(
            "Conciliação confirmada. SessaoId: {SessaoId}, lançados: {Lancados}, Em Conta: {EmConta}, divergência: {Divergencia}",
            sessionId, toPost.Count, balance.EmConta, balance.Divergencia);

        return new ConfirmReconciliationResultDto
        {
            PostedCount = toPost.Count,
            LinkedCount = linked.Count,
            IgnoredCount = items.Count(item => item.Status == ReconciliationItemStatus.Ignored),
            NettedCount = items.Count(item => item.Status == ReconciliationItemStatus.NettedInternal),
            EmConta = balance.EmConta,
            SaldoBancos = balance.SaldoBancos,
            Divergencia = balance.Divergencia ?? 0m
        };
    }

    /// <summary>
    /// Descarta a sessão e apaga seus itens, liberando os mesmos lançamentos
    /// para serem reimportados num sync futuro.
    /// </summary>
    public async Task<bool> DiscardAsync(Guid sessionId)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session is null)
            return false;

        if (session.Status != ReconciliationSessionStatus.Draft)
            throw new ArgumentException("Esta conciliação já foi encerrada.");

        await _repository.DeleteItemsBySessionAsync(sessionId);
        session.Discard();
        await _repository.UpdateSessionAsync(session);

        return true;
    }

    public async Task<IEnumerable<ReconciliationItemDto>> GetIgnoredItemsAsync()
    {
        var items = (await _repository.GetIgnoredItemsAsync()).ToList();
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        var referenced = await LoadReferencedTransactionsAsync(items);
        return items.Select(item => MapItemToDto(item, categories, referenced));
    }

    internal static DateTime ResolveStartDate(BankAccount account, DateTime? requestedFrom)
    {
        // O cursor da conta manda; a data pedida só vale no primeiro sync.
        // Reprocessa o próprio dia do último sync de propósito: um lançamento pode cair
        // depois que a conciliação rodou. A reimportação é inofensiva porque o ExternalId
        // tem índice único e o dedupe descarta o que já é conhecido.
        if (account.LastSyncedAt.HasValue)
            return account.LastSyncedAt.Value.Date;

        return requestedFrom.HasValue
            ? BankStatementNormalizer.ToCalendarDate(requestedFrom.Value)
            : DateTime.UtcNow.AddDays(-30).Date;
    }

    private static ReconciliationItem BuildItem(Guid sessionId, CollectedTransaction collected)
    {
        var transaction = collected.Transaction;
        var payment = transaction.PaymentData;

        // O contraparte pode vir do paymentData (Santander) ou embutido na descrição (Inter).
        var (_, nameFromDescription) = BankStatementNormalizer.ExtractCounterparty(transaction.Description);
        var counterpartyName = transaction.Amount < 0
            ? payment?.Receiver?.Name ?? nameFromDescription
            : payment?.Payer?.Name ?? nameFromDescription;

        var counterpartyDocument = transaction.Amount < 0
            ? payment?.Receiver?.DocumentNumber?.Value
            : payment?.Payer?.DocumentNumber?.Value;

        return new ReconciliationItem(
            sessionId,
            collected.Account.Id,
            transaction.Id,
            BankStatementNormalizer.ToLedgerDescription(
                transaction.Description ?? transaction.DescriptionRaw, 500),
            transaction.Amount,
            transaction.Date,
            transaction.Category,
            counterpartyName,
            counterpartyDocument,
            payment?.PaymentMethod);
    }

    private InternalTransferAnalysis ApplyInternalTransferRules(
        List<ReconciliationItem> items,
        List<BankAccount> accounts)
    {
        var holderNames = accounts.Select(account => account.HolderName).ToList();

        // O CPF do titular não é cadastrado em lugar nenhum, então a detecção por documento
        // só entra quando o nome já confirmou a titularidade em alguma perna da transferência.
        var confirmedDocuments = items
            .Where(item => holderNames.Any(holder =>
                TextSearchNormalizer.CalculateSimilarity(holder, item.CounterpartyName) >= 0.95m))
            .Select(item => item.CounterpartyDocument)
            .Where(document => !string.IsNullOrWhiteSpace(document))
            .Select(document => document!)
            .Distinct()
            .ToList();

        var analysis = _transferMatcher.Analyze(items, holderNames, confirmedDocuments);

        foreach (var pair in analysis.Pairs)
        {
            pair.Outflow.MarkAsInternalTransfer(pair.Inflow.Id);
            pair.Inflow.MarkAsInternalTransfer(pair.Outflow.Id);
        }

        foreach (var orphan in analysis.Orphans)
        {
            orphan.Flag(ReconciliationReviewFlag.OrphanTransfer);
        }

        return analysis;
    }

    private async Task ApplyCategoryProposalsAsync(List<ReconciliationItem> items)
    {
        foreach (var item in items)
        {
            if (item.Status == ReconciliationItemStatus.NettedInternal)
                continue;

            var description = BankStatementNormalizer.ToLedgerDescription(item.RawDescription);
            var resolution = await _categoryResolutionService.ResolveAsync(description, null);

            if (resolution.IsResolved && resolution.Category is not null)
            {
                item.ApplyProposal(description, resolution.Category.Id);
                continue;
            }

            item.ApplyProposal(description, null);

            // Transferência órfã já tem aviso próprio, mais específico que "categoria indefinida".
            if (item.ReviewFlag == ReconciliationReviewFlag.None)
                item.Flag(ReconciliationReviewFlag.AmbiguousCategory);
        }
    }

    /// <summary>
    /// O bot do Telegram e a tela de transações criam lançamentos livremente. Se um gasto já foi
    /// registrado à mão e também vier do banco, ele duplica — por isso a suspeita vai para a revisão.
    /// </summary>
    private async Task FlagPossibleDuplicatesAsync(List<ReconciliationItem> items)
    {
        var candidates = items
            .Where(item => item.Status != ReconciliationItemStatus.NettedInternal)
            .ToList();

        if (candidates.Count == 0)
            return;

        var from = candidates.Min(item => item.RawDate).AddDays(-40);
        var to = candidates.Max(item => item.RawDate).AddDays(40);
        var existing = (await _transactionRepository.GetByDueDateRangeAsync(from, to)).ToList();

        if (existing.Count == 0)
            return;

        var noLinksYet = new Dictionary<Guid, decimal>();

        foreach (var item in candidates)
        {
            // O ranqueamento pesa valor, data e descrição — assim "GOOGLE BRASIL" encontra
            // "Google Drive - Anual" e não confunde "PIZZARIA DUOS" com um investimento de
            // mesmo valor no mesmo dia.
            var best = _matchFinder.Find(item, existing, noLinksYet).FirstOrDefault();
            if (best is null)
                continue;

            // Só candidatos fortes viram sugestão nomeada na tela. Um palpite fraco daria
            // um botão "Vincular a <lançamento aleatório>", que atrapalha mais do que ajuda —
            // os demais continuam disponíveis dentro do diálogo, ranqueados.
            if (!best.IsStrong)
                continue;

            // Mesmo forte, o vínculo nunca é automático: a decisão fica com o usuário.
            item.SuggestLink(best.Transaction.Id);

            if (item.ReviewFlag == ReconciliationReviewFlag.None)
                item.Flag(ReconciliationReviewFlag.PossibleDuplicate);
        }
    }

    private async Task<ReconciliationSessionDto> BuildSessionDtoAsync(
        ReconciliationSession session,
        List<ReconciliationItem> items)
    {
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        var referenced = await LoadReferencedTransactionsAsync(items);
        var balance = await _balanceService.GetCurrentBalanceAsync();

        var postable = items
            .Where(item => item.Status is ReconciliationItemStatus.Approved or ReconciliationItemStatus.Pending)
            .ToList();

        var income = postable
            .Where(item => item.ProposedType == TransactionType.ReceivableBill)
            .Sum(item => item.AbsoluteAmount);

        var expenses = postable
            .Where(item => item.ProposedType == TransactionType.PayableBill)
            .Sum(item => item.AbsoluteAmount);

        var dto = new ReconciliationSessionDto
        {
            Id = session.Id,
            PeriodFrom = session.PeriodFrom,
            PeriodTo = session.PeriodTo,
            Status = session.Status,
            StartedAt = session.StartedAt,
            ClosedAt = session.ClosedAt,
            TotalItems = items.Count,
            NeedsAttentionCount = items.Count(item => item.ReviewFlag != ReconciliationReviewFlag.None),
            ReadyCount = items.Count(item =>
                item.ReviewFlag == ReconciliationReviewFlag.None &&
                item.Status is ReconciliationItemStatus.Approved or ReconciliationItemStatus.Pending),
            NettedCount = items.Count(item => item.Status == ReconciliationItemStatus.NettedInternal),
            TotalIncome = income,
            TotalExpenses = expenses,
            BankBalance = balance.SaldoBancos,
            ProjectedBalance = balance.EmConta + income - expenses,
            Items = items.Select(item => MapItemToDto(item, categories, referenced)).ToList()
        };

        if (dto.ProjectedBalance != dto.BankBalance)
        {
            dto.Warnings.Add(
                $"Depois de confirmar, o consolidado ficará em {dto.ProjectedBalance.ToString("C", PtBrCulture)} " +
                $"contra {dto.BankBalance.ToString("C", PtBrCulture)} somados nas contas.");
        }

        return dto;
    }

    private static ReconciliationItemDto MapItemToDto(
        ReconciliationItem item,
        List<Category> categories,
        IReadOnlyDictionary<Guid, FinancialTransaction> referenced) => new()
    {
        Id = item.Id,
        BankAccountId = item.BankAccountId,
        BankAccountName = item.BankAccount?.Name ?? string.Empty,
        RawDescription = item.RawDescription,
        RawAmount = item.RawAmount,
        AbsoluteAmount = item.AbsoluteAmount,
        RawDate = item.RawDate,
        RawCategory = item.RawCategory,
        CounterpartyName = item.CounterpartyName,
        PaymentMethod = item.PaymentMethod,
        ProposedDescription = item.ProposedDescription,
        ProposedCategoryId = item.ProposedCategoryId,
        ProposedCategoryName = categories.FirstOrDefault(category => category.Id == item.ProposedCategoryId)?.Name,
        ProposedType = item.ProposedType,
        Status = item.Status,
        ReviewFlag = item.ReviewFlag,
        MatchedItemId = item.MatchedItemId,
        LinkedTransactionId = item.LinkedTransactionId,
        LinkedTransactionDescription = Lookup(referenced, item.LinkedTransactionId)?.Description,
        LinkedTransactionAmount = Lookup(referenced, item.LinkedTransactionId)?.Amount,
        LinkedTransactionDueDate = Lookup(referenced, item.LinkedTransactionId)?.DueDate,
        LinkUpdateAmount = item.LinkUpdateAmount,
        LinkUpdateDueDate = item.LinkUpdateDueDate,
        LinkMarkAsPaid = item.LinkMarkAsPaid,
        SuggestedTransactionId = item.SuggestedTransactionId,
        SuggestedTransactionDescription = Lookup(referenced, item.SuggestedTransactionId)?.Description,
        ReviewReason = DescribeReviewFlag(item.ReviewFlag)
    };

    private static FinancialTransaction? Lookup(
        IReadOnlyDictionary<Guid, FinancialTransaction> referenced,
        Guid? id)
    {
        return id.HasValue && referenced.TryGetValue(id.Value, out var transaction) ? transaction : null;
    }

    private static string? DescribeReviewFlag(ReconciliationReviewFlag flag) => flag switch
    {
        ReconciliationReviewFlag.AmbiguousCategory => "Categoria indefinida — escolha antes de confirmar.",
        ReconciliationReviewFlag.OrphanTransfer =>
            "Parece transferência entre suas contas, mas o outro lado não apareceu neste período. " +
            "Anule só se tiver certeza; caso contrário, lance normalmente.",
        ReconciliationReviewFlag.PossibleDuplicate =>
            "Parece já existir um lançamento equivalente. Vincule para corrigir o que existe " +
            "em vez de criar um duplicado.",
        _ => null
    };

    private sealed record CollectedTransaction(BankAccount Account, PluggyTransaction Transaction);
}
