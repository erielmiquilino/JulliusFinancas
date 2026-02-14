using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CardService
{
    private readonly ICardRepository _repository;
    private readonly IFinancialTransactionRepository _financialTransactionRepository;
    private readonly ICardTransactionRepository _cardTransactionRepository;
    private readonly ILogger<CardService> _logger;

    public CardService(
        ICardRepository repository, 
        IFinancialTransactionRepository financialTransactionRepository, 
        ICardTransactionRepository cardTransactionRepository,
        ILogger<CardService> logger)
    {
        _repository = repository;
        _financialTransactionRepository = financialTransactionRepository;
        _cardTransactionRepository = cardTransactionRepository;
        _logger = logger;
    }

    public async Task<Card> CreateCardAsync(CreateCardRequest request)
    {
        _logger.LogInformation("Iniciando criação de cartão no serviço. " +
            "Nome: {Nome}, Banco: {Banco}, Dia fechamento: {DiaFechamento}, Dia vencimento: {DiaVencimento}, Limite: {Limite}",
            request.Name, request.IssuingBank, request.ClosingDay, request.DueDay, request.Limit);

        // Validações de negócio
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Tentativa de criar cartão com nome inválido");
            throw new ArgumentException("Nome do cartão é obrigatório");
        }

        if (string.IsNullOrWhiteSpace(request.IssuingBank))
        {
            _logger.LogWarning("Tentativa de criar cartão com banco emissor inválido");
            throw new ArgumentException("Banco emissor é obrigatório");
        }

        if (request.ClosingDay < 1 || request.ClosingDay > 31)
        {
            _logger.LogWarning("Tentativa de criar cartão com dia de fechamento inválido: {DiaFechamento}", 
                request.ClosingDay);
            throw new ArgumentException("Dia de fechamento deve estar entre 1 e 31");
        }

        if (request.DueDay < 1 || request.DueDay > 31)
        {
            _logger.LogWarning("Tentativa de criar cartão com dia de vencimento inválido: {DiaVencimento}", 
                request.DueDay);
            throw new ArgumentException("Dia de vencimento deve estar entre 1 e 31");
        }

        var card = new Card(
            request.Name,
            request.IssuingBank,
            request.ClosingDay,
            request.DueDay,
            request.Limit
        );

        var createdCard = await _repository.CreateAsync(card);
        
        _logger.LogInformation("Cartão criado com sucesso no serviço. " +
            "ID: {CartaoId}, Nome: {Nome}, Banco: {Banco}",
            createdCard.Id, createdCard.Name, createdCard.IssuingBank);

        return createdCard;
    }

    public async Task<Card?> GetCardByIdAsync(Guid id)
    {
        _logger.LogDebug("Buscando cartão por ID no serviço: {CartaoId}", id);
        
        var card = await _repository.GetByIdAsync(id);
        
        if (card == null)
        {
            _logger.LogWarning("Cartão não encontrado no serviço para ID: {CartaoId}", id);
        }
        else
        {
            _logger.LogDebug("Cartão encontrado no serviço. ID: {CartaoId}, Nome: {Nome}", 
                card.Id, card.Name);
        }
        
        return card;
    }

    public async Task<IEnumerable<Card>> GetAllCardsAsync()
    {
        _logger.LogDebug("Buscando todos os cartões no serviço");
        
        var cards = await _repository.GetAllAsync();
        var cardsList = cards.ToList();
        
        _logger.LogInformation("Busca de cartões concluída no serviço. Total encontrado: {TotalCartoes}", 
            cardsList.Count);
        
        return cardsList;
    }

    public async Task<Card?> UpdateCardAsync(Guid id, UpdateCardRequest request)
    {
        _logger.LogInformation("Iniciando atualização de cartão no serviço. " +
            "ID: {CartaoId}, Dados: {@Request}", id, request);

        var card = await _repository.GetByIdAsync(id);
        if (card == null)
        {
            _logger.LogWarning("Tentativa de atualizar cartão inexistente no serviço. ID: {CartaoId}", id);
            return null;
        }

        var oldLimit = card.Limit;

        card.UpdateDetails(
            request.Name,
            request.IssuingBank,
            request.ClosingDay,
            request.DueDay,
            request.Limit
        );

        // Se o limite total foi alterado, recalcula o limite disponível
        if (oldLimit != request.Limit)
        {
            _logger.LogInformation("Limite do cartão alterado de {LimiteAntigo} para {LimiteNovo}. " +
                "Recalculando limite disponível. CartaoId: {CartaoId}",
                oldLimit, request.Limit, id);
            await RecalculateCurrentLimitAsync(card);
        }

        await _repository.UpdateAsync(card);
        
        _logger.LogInformation("Cartão atualizado com sucesso no serviço. " +
            "ID: {CartaoId}, Nome: {Nome}, NovoLimite: {Limite}",
            card.Id, card.Name, card.Limit);
        
        return card;
    }

    public async Task<bool> DeleteCardAsync(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de cartão no serviço. ID: {CartaoId}", id);

        var card = await _repository.GetByIdAsync(id);
        if (card == null)
        {
            _logger.LogWarning("Tentativa de excluir cartão inexistente no serviço. ID: {CartaoId}", id);
            return false;
        }

        _logger.LogInformation("Excluindo faturas relacionadas ao cartão. CartaoId: {CartaoId}", id);
        // Exclui todas as faturas relacionadas ao cartão
        await DeleteCardInvoicesAsync(id);

        // Exclui o cartão (as CardTransactions serão excluídas automaticamente por cascade)
        await _repository.DeleteAsync(id);
        
        _logger.LogInformation("Cartão excluído com sucesso no serviço. ID: {CartaoId}", id);
        return true;
    }

    private async Task DeleteCardInvoicesAsync(Guid cardId)
    {
        _logger.LogDebug("Buscando faturas relacionadas ao cartão para exclusão. CartaoId: {CartaoId}", cardId);
        
        // Busca todas as faturas relacionadas ao cartão usando CardId
        var cardInvoices = await _financialTransactionRepository.GetByCardIdAsync(cardId);
        var invoicesList = cardInvoices.ToList();

        _logger.LogInformation("Encontradas {TotalFaturas} faturas para exclusão. CartaoId: {CartaoId}",
            invoicesList.Count, cardId);

        // Exclui cada fatura encontrada
        foreach (var invoice in invoicesList)
        {
            _logger.LogDebug("Excluindo fatura. FaturaId: {FaturaId}, CartaoId: {CartaoId}",
                invoice.Id, cardId);
            await _financialTransactionRepository.DeleteAsync(invoice.Id);
        }
        
        _logger.LogInformation("Todas as faturas do cartão foram excluídas. CartaoId: {CartaoId}", cardId);
    }

    private async Task RecalculateCurrentLimitAsync(Card card)
    {
        _logger.LogDebug("Recalculando limite disponível do cartão. CartaoId: {CartaoId}, LimiteTotal: {LimiteTotal}",
            card.Id, card.Limit);

        // Calcula qual é a fatura atual baseada nos dias de fechamento e vencimento
        var currentInvoice = CalculateCurrentInvoicePeriod(card.ClosingDay, card.DueDay);

        // Busca todas as transações da fatura atual em diante
        var futureTransactions = await _cardTransactionRepository
            .GetByCardIdFromPeriodAsync(card.Id, currentInvoice.Month, currentInvoice.Year);

        // Calcula o total usado (despesas positivas, receitas negativas)
        var totalUsed = futureTransactions.Sum(t => 
            t.Type == CardTransactionType.Expense ? t.Amount : -t.Amount);

        // Recalcula o limite disponível: limite total - valor usado
        var newCurrentLimit = card.Limit - totalUsed;
        
        card.SetCurrentLimit(newCurrentLimit);
    }

    /// <summary>
    /// Calcula o período da fatura atual baseado na data de hoje e nos dias de fechamento/vencimento do cartão.
    /// Replica a lógica do método calculateCurrentInvoicePeriod do frontend.
    /// </summary>
    private (int Year, int Month) CalculateCurrentInvoicePeriod(int closingDay, int dueDay)
    {
        var today = DateTime.Today;

        DateTime effectiveClosingDate;

        if (today.Day > closingDay)
            // Se hoje é depois do dia de fechamento, a data efetiva de fechamento é no próximo mês
            effectiveClosingDate = new DateTime(today.Year, today.Month, closingDay).AddMonths(1);
        else
            // Se hoje é antes ou no dia de fechamento, a data efetiva é neste mês
            effectiveClosingDate = new DateTime(today.Year, today.Month, closingDay);

        DateTime invoiceDueDate;

        if (dueDay <= closingDay)
        {
            // Se o vencimento é antes ou no dia de fechamento, vai para o próximo mês
            var monthOfDueDate = effectiveClosingDate.AddMonths(1);
            invoiceDueDate = new DateTime(monthOfDueDate.Year, monthOfDueDate.Month, dueDay);
        }
        else
            // Se o vencimento é depois do fechamento, fica no mesmo mês do fechamento
            invoiceDueDate = new DateTime(effectiveClosingDate.Year, effectiveClosingDate.Month, dueDay);

        return (invoiceDueDate.Year, invoiceDueDate.Month);
    }
} 