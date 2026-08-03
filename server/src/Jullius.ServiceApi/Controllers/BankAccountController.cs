using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Jullius.ServiceApi.Integrations.Pluggy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BankAccountController : ODataController
{
    private readonly BankAccountService _service;
    private readonly ConsolidatedBalanceService _balanceService;
    private readonly ILogger<BankAccountController> _logger;

    public BankAccountController(
        BankAccountService service,
        ConsolidatedBalanceService balanceService,
        ILogger<BankAccountController> logger)
    {
        _service = service;
        _balanceService = balanceService;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de contas bancárias");

        var accounts = await _service.GetAllAsync();

        _logger.LogInformation("Busca de contas bancárias concluída. Total encontrado: {TotalContas}",
            accounts.Count());

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _service.GetByIdAsync(id);

        if (account == null)
        {
            _logger.LogWarning("Conta bancária não encontrada para ID: {ContaId}", id);
            return NotFound();
        }

        return Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountRequest request)
    {
        _logger.LogInformation("Iniciando cadastro de conta bancária. Instituição: {Instituicao}, Nome: {Nome}",
            request.Institution, request.Name);

        try
        {
            var account = await _service.CreateAsync(request);

            _logger.LogInformation("Conta bancária cadastrada com sucesso. ID: {ContaId}", account.Id);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha no cadastro da conta bancária. Erro: {Erro}. Request: {@Request}",
                ex.Message, request);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankAccountRequest request)
    {
        try
        {
            var account = await _service.UpdateAsync(id, request);

            if (account == null)
            {
                _logger.LogWarning("Tentativa de atualizar conta bancária inexistente. ID: {ContaId}", id);
                return NotFound();
            }

            _logger.LogInformation("Conta bancária atualizada com sucesso. ID: {ContaId}", id);
            return Ok(account);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização da conta bancária. ID: {ContaId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);

        if (!success)
        {
            _logger.LogWarning("Tentativa de excluir conta bancária inexistente. ID: {ContaId}", id);
            return NotFound();
        }

        _logger.LogInformation("Conta bancária excluída com sucesso. ID: {ContaId}", id);
        return NoContent();
    }

    /// <summary>Lista as contas que a Pluggy expõe para um item, para preencher o cadastro.</summary>
    [HttpGet("discover/{pluggyItemId}")]
    public async Task<IActionResult> Discover(string pluggyItemId)
    {
        _logger.LogInformation("Iniciando descoberta de contas na Pluggy. ItemId: {ItemId}", pluggyItemId);

        try
        {
            var accounts = await _service.DiscoverAccountsAsync(pluggyItemId);

            _logger.LogInformation("Descoberta concluída. Contas encontradas: {TotalContas}", accounts.Count());
            return Ok(accounts);
        }
        catch (PluggyItemNotFoundException ex)
        {
            _logger.LogWarning("Item inexistente na Pluggy. ItemId: {ItemId}", pluggyItemId);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Falha ao consultar a Pluggy. Erro: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/opening-balance")]
    public async Task<IActionResult> SetOpeningBalance(Guid id, [FromBody] SetOpeningBalanceRequest request)
    {
        _logger.LogInformation("Iniciando definição de marco zero. ContaId: {ContaId}, Data: {Data}",
            id, request.OpeningBalanceDate);

        try
        {
            var account = await _service.SetOpeningBalanceAsync(id, request);

            _logger.LogInformation("Marco zero definido. ContaId: {ContaId}, Abertura: {Abertura}",
                id, account.OpeningBalance);

            return Ok(account);
        }
        catch (PluggyItemNotFoundException ex)
        {
            _logger.LogWarning("Conexão perdida ao definir marco zero. ContaId: {ContaId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao definir marco zero. ContaId: {ContaId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Falha ao consultar a Pluggy. ContaId: {ContaId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/opening-balance")]
    public async Task<IActionResult> ClearOpeningBalance(Guid id)
    {
        var account = await _service.ClearOpeningBalanceAsync(id);

        if (account == null)
        {
            _logger.LogWarning("Tentativa de remover marco zero de conta inexistente. ID: {ContaId}", id);
            return NotFound();
        }

        _logger.LogInformation("Marco zero removido. ContaId: {ContaId}", id);
        return Ok(account);
    }

    [HttpGet("connections")]
    public async Task<IActionResult> CheckConnections()
    {
        _logger.LogInformation("Iniciando verificação das conexões da Pluggy");

        var accounts = await _service.CheckConnectionsAsync();
        return Ok(accounts);
    }

    /// <summary>Saldo consolidado ("Em Conta") acumulado do marco zero até o fim do mês informado.</summary>
    [HttpGet("consolidated-balance")]
    public async Task<IActionResult> GetConsolidatedBalance([FromQuery] int month, [FromQuery] int year)
    {
        if (month is < 1 or > 12)
            return BadRequest(new { message = "Mês deve estar entre 1 e 12" });

        if (year is < 2000 or > 2100)
            return BadRequest(new { message = "Ano deve estar entre 2000 e 2100" });

        var balance = await _balanceService.GetBalanceAsync(month, year);
        return Ok(balance);
    }
}
