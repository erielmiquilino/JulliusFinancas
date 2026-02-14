using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Jullius.Domain.Domain.Entities;

using Microsoft.AspNetCore.Authorization;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FinancialTransactionController : ODataController
{
    private readonly FinancialTransactionService _service;
    private readonly ILogger<FinancialTransactionController> _logger;

    public FinancialTransactionController(FinancialTransactionService service, ILogger<FinancialTransactionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialTransactionRequest request)
    {
        _logger.LogInformation("Iniciando criação de transação financeira. " +
            "Tipo: {Tipo}, Valor: {Valor}, Data: {Data}, Parcelada: {EhParcelada}",
            request.Type, request.Amount, request.DueDate, request.IsInstallment);

        try
        {
            var transactions = await _service.CreateTransactionAsync(request);
            var transactionsList = transactions.ToList();
            
            _logger.LogInformation("Transação(ões) criada(s) com sucesso. " +
                "Total de transações: {TotalTransacoes}, Primeira transação ID: {PrimeiraTransacaoId}",
                transactionsList.Count, transactionsList.First().Id);
            
            if (transactionsList.Count == 1)
            {
                // Se é apenas uma transação, retorna como antes para compatibilidade
                var transaction = transactionsList.First();
                return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
            }
            else
            {
                // Se são múltiplas transações (parcelado), retorna a lista
                _logger.LogInformation("Transações parceladas criadas. Total de parcelas: {TotalParcelas}", 
                    transactionsList.Count);
                return Ok(transactionsList);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na criação da transação devido a dados inválidos. " +
                "Erro: {Erro}. Request: {@Request}", ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todas as transações financeiras");

        var transactions = await _service.GetAllTransactionsAsync();
        
        _logger.LogInformation("Busca de transações concluída. Total encontrado: {TotalTransacoes}", 
            transactions.Count());
        
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Iniciando busca de transação por ID: {TransacaoId}", id);

        var transaction = await _service.GetTransactionByIdAsync(id);
        
        if (transaction == null)
        {
            _logger.LogWarning("Transação não encontrada para ID: {TransacaoId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Transação encontrada com sucesso. " +
            "ID: {TransacaoId}, Tipo: {Tipo}, Valor: {Valor}",
            transaction.Id, transaction.Type, transaction.Amount);
            
        return Ok(transaction);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de transação. ID: {TransacaoId}", id);

        var result = await _service.DeleteTransactionAsync(id);
        
        if (!result)
        {
            _logger.LogWarning("Tentativa de excluir transação inexistente. ID: {TransacaoId}", id);
            return NotFound();
        }

        _logger.LogInformation("Transação excluída com sucesso. ID: {TransacaoId}", id);
        return NoContent();
    }

    [HttpPost("delete-batch")]
    public async Task<IActionResult> DeleteBatch([FromBody] List<Guid> ids)
    {
        _logger.LogInformation("Iniciando exclusão em lote de transações. Total: {Total}", ids.Count);

        if (ids == null || ids.Count == 0)
            return BadRequest("Nenhum ID fornecido.");

        var deleted = await _service.DeleteTransactionsAsync(ids);

        _logger.LogInformation("Exclusão em lote concluída. Total excluído: {Total}", deleted);
        return Ok(new { deletedCount = deleted });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialTransactionRequest request)
    {
        _logger.LogInformation("Iniciando atualização de transação. " +
            "ID: {TransacaoId}, Dados: {@Request}", id, request);

        try
        {
            var transaction = await _service.UpdateTransactionAsync(id, request);
            
            if (transaction == null)
            {
                _logger.LogWarning("Tentativa de atualizar transação inexistente. ID: {TransacaoId}", id);
                return NotFound();
            }
                
            _logger.LogInformation("Transação atualizada com sucesso. " +
                "ID: {TransacaoId}, Tipo: {Tipo}, Valor: {Valor}",
                transaction.Id, transaction.Type, transaction.Amount);
                
            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização da transação devido a dados inválidos. " +
                "ID: {TransacaoId}, Erro: {Erro}. Request: {@Request}", id, ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id}/payment-status")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] bool isPaid)
    {
        _logger.LogInformation("Iniciando atualização de status de pagamento. " +
            "TransacaoId: {TransacaoId}, NovoStatus: {StatusPagamento}",
            id, isPaid ? "Pago" : "Pendente");

        var transaction = await _service.UpdatePaymentStatusAsync(id, isPaid);
        
        if (transaction == null)
        {
            _logger.LogWarning("Tentativa de atualizar status de transação inexistente. ID: {TransacaoId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Status de pagamento atualizado com sucesso. " +
            "TransacaoId: {TransacaoId}, StatusAtual: {StatusPagamento}",
            transaction.Id, transaction.IsPaid ? "Pago" : "Pendente");
            
        return Ok(transaction);
    }

    [HttpPost("pay-with-card")]
    public async Task<IActionResult> PayWithCard([FromBody] PayWithCardRequest request)
    {
        _logger.LogInformation("Iniciando pagamento com cartão. " +
            "CartaoId: {CartaoId}, ValorCartao: {ValorCartao}, QtdTransacoes: {QtdTransacoes}",
            request.CardId, request.CardAmount, request.TransactionIds.Count);

        try
        {
            var response = await _service.PayWithCardAsync(request);
            
            _logger.LogInformation("Pagamento com cartão realizado com sucesso. " +
                "TransacoesPagas: {TransacoesPagas}, ReceitaId: {ReceitaId}, DespesaCartaoIds: {DespesaIds}",
                response.PaidTransactionsCount, response.IncomeTransactionId, 
                string.Join(", ", response.CardTransactionIds));
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha no pagamento com cartão devido a dados inválidos. " +
                "Erro: {Erro}. Request: {@Request}", ex.Message, request);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar pagamento com cartão. Request: {@Request}", request);
            return StatusCode(500, "Erro ao processar pagamento");
        }
    }
} 