using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

using Microsoft.AspNetCore.Authorization;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CardTransactionController : ODataController
{
    private readonly CardTransactionService _service;
    private readonly ILogger<CardTransactionController> _logger;

    public CardTransactionController(CardTransactionService service, ILogger<CardTransactionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCardTransactionRequest request)
    {
        _logger.LogInformation("Iniciando criação de transação de cartão. " +
            "CartaoId: {CartaoId}, Valor: {Valor}, Parcelas: {Parcelas}",
            request.CardId, request.Amount, request.InstallmentCount);

        try
        {
            var cardTransactions = await _service.CreateCardTransactionAsync(request);
            var transactionsList = cardTransactions.ToList();
            
            _logger.LogInformation("Transações de cartão criadas com sucesso. " +
                "Total: {TotalTransacoes}, CartaoId: {CartaoId}",
                transactionsList.Count, request.CardId);
            
            if (transactionsList.Count == 1)
            {
                // Retorna uma única transação
                return CreatedAtAction(nameof(GetById), new { id = transactionsList.First().Id }, transactionsList.First());
            }
            else
            {
                // Retorna múltiplas transações (parceladas)
                return Ok(new { 
                    message = $"{transactionsList.Count} parcelas criadas com sucesso",
                    transactions = transactionsList 
                });
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in {Operation} for {EntityType} with parameters {@Parameters}", 
                nameof(Create), "CardTransaction", new { request.CardId, request.Amount });
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with parameters {@Parameters}", 
                nameof(Create), "CardTransaction", new { request.CardId, request.Amount });
            return StatusCode(500, "An error occurred while creating the transaction");
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todas as transações de cartão");

        try
        {
            var cardTransactions = await _service.GetAllCardTransactionsAsync();
            _logger.LogInformation("Busca concluída com sucesso. Total de transações: {TotalTransacoes}", 
                cardTransactions.Count());
            return Ok(cardTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar todas as transações de cartão");
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Starting {Operation} for {EntityType} with Id {EntityId}", 
            nameof(GetById), "CardTransaction", id);

        try
        {
            var cardTransaction = await _service.GetCardTransactionByIdAsync(id);
            if (cardTransaction == null)
            {
                _logger.LogWarning("Card transaction with Id {EntityId} not found", id);
                return NotFound();
            }
                
            _logger.LogInformation("Successfully retrieved card transaction with Id {EntityId}", id);
            return Ok(cardTransaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with Id {EntityId}", 
                nameof(GetById), "CardTransaction", id);
            return StatusCode(500, "An error occurred while retrieving the transaction");
        }
    }

    [HttpGet("card/{cardId}")]
    public async Task<IActionResult> GetByCardId(Guid cardId)
    {
        _logger.LogInformation("Starting {Operation} for {EntityType} with CardId {CardId}", 
            nameof(GetByCardId), "CardTransaction", cardId);

        try
        {
            var cardTransactions = await _service.GetCardTransactionsByCardIdAsync(cardId);
            _logger.LogInformation("Successfully retrieved {Count} card transactions for CardId {CardId}", 
                cardTransactions.Count(), cardId);
            return Ok(cardTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with CardId {CardId}", 
                nameof(GetByCardId), "CardTransaction", cardId);
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }

    [HttpGet("card/{cardId}/invoice/{year}/{month}")]
    public async Task<IActionResult> GetForInvoice(Guid cardId, int year, int month)
    {
        _logger.LogInformation("Starting {Operation} for {EntityType} with CardId {CardId}, Year {Year}, Month {Month}", 
            nameof(GetForInvoice), "CardTransaction", cardId, year, month);

        try
        {
            var cardTransactions = await _service.GetCardTransactionsForInvoiceAsync(cardId, month, year);
            _logger.LogInformation("Successfully retrieved {Count} card transactions for invoice CardId {CardId}, {Year}-{Month:D2}", 
                cardTransactions.Transactions.Count(), cardId, year, month);
            return Ok(cardTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with CardId {CardId}, Year {Year}, Month {Month}", 
                nameof(GetForInvoice), "CardTransaction", cardId, year, month);
            return StatusCode(500, "An error occurred while retrieving invoice transactions");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Starting {Operation} for {EntityType} with Id {EntityId}", 
            nameof(Delete), "CardTransaction", id);

        try
        {
            var result = await _service.DeleteCardTransactionAsync(id);
            if (!result)
            {
                _logger.LogWarning("Card transaction with Id {EntityId} not found for deletion", id);
                return NotFound();
            }

            _logger.LogInformation("Successfully deleted card transaction with Id {EntityId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with Id {EntityId}", 
                nameof(Delete), "CardTransaction", id);
            return StatusCode(500, "An error occurred while deleting the transaction");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCardTransactionRequest request)
    {
        _logger.LogInformation("Starting {Operation} for {EntityType} with Id {EntityId} and parameters {@Parameters}", 
            nameof(Update), "CardTransaction", id, new { request.Amount, request.Description });

        try
        {
            var cardTransaction = await _service.UpdateCardTransactionAsync(id, request);
            if (cardTransaction == null)
            {
                _logger.LogWarning("Card transaction with Id {EntityId} not found for update", id);
                return NotFound();
            }
                
            _logger.LogInformation("Successfully updated card transaction with Id {EntityId}", id);
            return Ok(cardTransaction);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in {Operation} for {EntityType} with Id {EntityId}", 
                nameof(Update), "CardTransaction", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Operation} for {EntityType} with Id {EntityId}", 
                nameof(Update), "CardTransaction", id);
            return StatusCode(500, "An error occurred while updating the transaction");
        }
    }
} 