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
public class OverdueAccountController : ODataController
{
    private readonly OverdueAccountService _service;
    private readonly ILogger<OverdueAccountController> _logger;

    public OverdueAccountController(OverdueAccountService service, ILogger<OverdueAccountController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOverdueAccountRequest request)
    {
        _logger.LogInformation("Iniciando criação de conta atrasada. Descrição: {Descricao}, Valor: {Valor}", 
            request.Description, request.CurrentDebtValue);

        try
        {
            var overdueAccount = await _service.CreateOverdueAccountAsync(request);
            
            _logger.LogInformation("Conta atrasada criada com sucesso. Id: {OverdueAccountId}, Descrição: {Descricao}", 
                overdueAccount.Id, overdueAccount.Description);
                
            return CreatedAtAction(nameof(GetById), new { id = overdueAccount.Id }, overdueAccount);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na criação da conta atrasada. Erro: {Erro}. Request: {@Request}", 
                ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todas as contas atrasadas");

        var overdueAccounts = await _service.GetAllOverdueAccountsAsync();
        
        _logger.LogInformation("Busca de contas atrasadas concluída. Total encontrado: {TotalOverdueAccounts}", 
            overdueAccounts.Count());
        
        return Ok(overdueAccounts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Iniciando busca de conta atrasada por ID: {OverdueAccountId}", id);

        var overdueAccount = await _service.GetOverdueAccountByIdAsync(id);
        
        if (overdueAccount == null)
        {
            _logger.LogWarning("Conta atrasada não encontrada para ID: {OverdueAccountId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Conta atrasada encontrada. ID: {OverdueAccountId}, Descrição: {Descricao}", 
            overdueAccount.Id, overdueAccount.Description);
            
        return Ok(overdueAccount);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de conta atrasada. ID: {OverdueAccountId}", id);

        var success = await _service.DeleteOverdueAccountAsync(id);
        
        if (!success)
        {
            _logger.LogWarning("Tentativa de excluir conta atrasada inexistente. ID: {OverdueAccountId}", id);
            return NotFound();
        }

        _logger.LogInformation("Conta atrasada excluída com sucesso. ID: {OverdueAccountId}", id);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOverdueAccountRequest request)
    {
        _logger.LogInformation("Iniciando atualização de conta atrasada. ID: {OverdueAccountId}, Dados: {@Request}", 
            id, request);

        try
        {
            var overdueAccount = await _service.UpdateOverdueAccountAsync(id, request);
            
            if (overdueAccount == null)
            {
                _logger.LogWarning("Tentativa de atualizar conta atrasada inexistente. ID: {OverdueAccountId}", id);
                return NotFound();
            }
                
            _logger.LogInformation("Conta atrasada atualizada com sucesso. ID: {OverdueAccountId}, Descrição: {Descricao}", 
                overdueAccount.Id, overdueAccount.Description);
                
            return Ok(overdueAccount);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização da conta atrasada. ID: {OverdueAccountId}, Erro: {Erro}. Request: {@Request}", 
                id, ex.Message, request);
            return BadRequest(ex.Message);
        }
    }
}
