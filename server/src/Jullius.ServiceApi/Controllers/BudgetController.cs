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
public class BudgetController : ODataController
{
    private readonly BudgetService _service;
    private readonly ILogger<BudgetController> _logger;

    public BudgetController(BudgetService service, ILogger<BudgetController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        _logger.LogInformation("Iniciando criação de budget. Nome: {Nome}, Mês: {Mes}, Ano: {Ano}", 
            request.Name, request.Month, request.Year);

        try
        {
            var budget = await _service.CreateBudgetAsync(request);
            
            _logger.LogInformation("Budget criado com sucesso. Id: {BudgetId}, Nome: {Nome}", 
                budget.Id, budget.Name);
                
            return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na criação do budget. Erro: {Erro}. Request: {@Request}", 
                ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todos os budgets");

        var budgets = await _service.GetAllBudgetsAsync();
        
        _logger.LogInformation("Busca de budgets concluída. Total encontrado: {TotalBudgets}", 
            budgets.Count());
        
        return Ok(budgets);
    }

    [HttpGet("by-period")]
    public async Task<IActionResult> GetByPeriod([FromQuery] int month, [FromQuery] int year)
    {
        _logger.LogInformation("Iniciando busca de budgets por período. Mês: {Mes}, Ano: {Ano}", month, year);

        var budgets = await _service.GetBudgetsByMonthAndYearAsync(month, year);
        
        _logger.LogInformation("Busca de budgets por período concluída. Total encontrado: {TotalBudgets}", 
            budgets.Count());
        
        return Ok(budgets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Iniciando busca de budget por ID: {BudgetId}", id);

        var budget = await _service.GetBudgetByIdAsync(id);
        
        if (budget == null)
        {
            _logger.LogWarning("Budget não encontrado para ID: {BudgetId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Budget encontrado. ID: {BudgetId}, Nome: {Nome}", 
            budget.Id, budget.Name);
            
        return Ok(budget);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de budget. ID: {BudgetId}", id);

        var (success, errorMessage) = await _service.DeleteBudgetAsync(id);
        
        if (!success)
        {
            if (errorMessage == "Budget não encontrado")
            {
                _logger.LogWarning("Tentativa de excluir budget inexistente. ID: {BudgetId}", id);
                return NotFound();
            }
            
            _logger.LogWarning("Não foi possível excluir budget. ID: {BudgetId}, Motivo: {Motivo}", 
                id, errorMessage);
            return BadRequest(errorMessage);
        }

        _logger.LogInformation("Budget excluído com sucesso. ID: {BudgetId}", id);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        _logger.LogInformation("Iniciando atualização de budget. ID: {BudgetId}, Dados: {@Request}", 
            id, request);

        try
        {
            var budget = await _service.UpdateBudgetAsync(id, request);
            
            if (budget == null)
            {
                _logger.LogWarning("Tentativa de atualizar budget inexistente. ID: {BudgetId}", id);
                return NotFound();
            }
                
            _logger.LogInformation("Budget atualizado com sucesso. ID: {BudgetId}, Nome: {Nome}", 
                budget.Id, budget.Name);
                
            return Ok(budget);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização do budget. ID: {BudgetId}, Erro: {Erro}. Request: {@Request}", 
                id, ex.Message, request);
            return BadRequest(ex.Message);
        }
    }
}

