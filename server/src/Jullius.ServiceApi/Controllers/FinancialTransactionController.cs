using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinancialTransactionController : ODataController
{
    private readonly FinancialTransactionService _service;

    public FinancialTransactionController(FinancialTransactionService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialTransactionRequest request)
    {
        try
        {
            var transactions = await _service.CreateTransactionAsync(request);
            var transactionsList = transactions.ToList();
            
            if (transactionsList.Count == 1)
            {
                // Se é apenas uma transação, retorna como antes para compatibilidade
                var transaction = transactionsList.First();
                return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
            }
            else
            {
                // Se são múltiplas transações (parcelado), retorna a lista
                return Ok(transactionsList);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 100)]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await _service.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var transaction = await _service.GetTransactionByIdAsync(id);
        if (transaction == null)
            return NotFound();
            
        return Ok(transaction);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteTransactionAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialTransactionRequest request)
    {
        try
        {
            var transaction = await _service.UpdateTransactionAsync(id, request);
            if (transaction == null)
                return NotFound();
                
            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id}/payment-status")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] bool isPaid)
    {
        var transaction = await _service.UpdatePaymentStatusAsync(id, isPaid);
        if (transaction == null)
            return NotFound();
            
        return Ok(transaction);
    }
} 