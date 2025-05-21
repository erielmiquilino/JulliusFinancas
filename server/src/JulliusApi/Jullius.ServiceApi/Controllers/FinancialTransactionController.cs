using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinancialTransactionController(FinancialTransactionService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialTransactionRequest request)
    {
        try
        {
            var transaction = await service.CreateTransactionAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transactions = await service.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var transaction = await service.GetTransactionByIdAsync(id);
        if (transaction == null)
            return NotFound();
            
        return Ok(transaction);
    }
} 