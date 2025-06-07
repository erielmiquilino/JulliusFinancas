using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardTransactionController : ODataController
{
    private readonly CardTransactionService _service;

    public CardTransactionController(CardTransactionService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCardTransactionRequest request)
    {
        try
        {
            var cardTransaction = await _service.CreateCardTransactionAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = cardTransaction.Id }, cardTransaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public async Task<IActionResult> GetAll()
    {
        var cardTransactions = await _service.GetAllCardTransactionsAsync();
        return Ok(cardTransactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cardTransaction = await _service.GetCardTransactionByIdAsync(id);
        if (cardTransaction == null)
            return NotFound();
            
        return Ok(cardTransaction);
    }

    [HttpGet("card/{cardId}")]
    public async Task<IActionResult> GetByCardId(Guid cardId)
    {
        var cardTransactions = await _service.GetCardTransactionsByCardIdAsync(cardId);
        return Ok(cardTransactions);
    }

    [HttpGet("card/{cardId}/invoice/{year}/{month}")]
    public async Task<IActionResult> GetForInvoice(Guid cardId, int year, int month)
    {
        var cardTransactions = await _service.GetCardTransactionsForInvoiceAsync(cardId, month, year);
        return Ok(cardTransactions);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteCardTransactionAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCardTransactionRequest request)
    {
        try
        {
            var cardTransaction = await _service.UpdateCardTransactionAsync(id, request);
            if (cardTransaction == null)
                return NotFound();
                
            return Ok(cardTransaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
} 