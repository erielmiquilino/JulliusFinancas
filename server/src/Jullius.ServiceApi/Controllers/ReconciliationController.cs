using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services.Reconciliation;
using Jullius.ServiceApi.Integrations.Pluggy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReconciliationController : ODataController
{
    private readonly ReconciliationService _service;
    private readonly ILogger<ReconciliationController> _logger;

    public ReconciliationController(ReconciliationService service, ILogger<ReconciliationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncReconciliationRequest request)
    {
        _logger.LogInformation("Iniciando sincronização de conciliação. A partir de: {DataInicial}", request.From);

        try
        {
            var result = await _service.SyncAsync(request);

            _logger.LogInformation(
                "Sincronização concluída. Importados: {Importados}, já conhecidos: {Pulados}, anulados: {Anulados}",
                result.ImportedCount, result.SkippedCount, result.NettedCount);

            return Ok(result);
        }
        catch (PluggyItemNotFoundException ex)
        {
            _logger.LogWarning("Conexão perdida durante a sincronização. Erro: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na sincronização. Erro: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar a Pluggy durante a sincronização.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Conciliação em aberto aguardando revisão, se houver.</summary>
    [HttpGet("sessions/open")]
    public async Task<IActionResult> GetOpenSession()
    {
        var session = await _service.GetOpenSessionAsync();

        if (session == null)
            return NoContent();

        return Ok(session);
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var session = await _service.GetSessionAsync(id);

        if (session == null)
        {
            _logger.LogWarning("Conciliação não encontrada. ID: {SessaoId}", id);
            return NotFound();
        }

        return Ok(session);
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateReconciliationItemRequest request)
    {
        try
        {
            var item = await _service.UpdateItemAsync(id, request);

            if (item == null)
            {
                _logger.LogWarning("Item de conciliação não encontrado. ID: {ItemId}", id);
                return NotFound();
            }

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao revisar item. ID: {ItemId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Lançamentos existentes que podem ser o mesmo evento desta linha do extrato.</summary>
    [HttpGet("items/{id}/match-candidates")]
    public async Task<IActionResult> GetMatchCandidates(Guid id, [FromQuery] string? search)
    {
        try
        {
            var candidates = await _service.GetMatchCandidatesAsync(id, search);
            return Ok(candidates);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao buscar candidatos. ItemId: {ItemId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Aponta a linha para um lançamento existente, em vez de criar um novo.</summary>
    [HttpPost("items/{id}/link")]
    public async Task<IActionResult> LinkItem(Guid id, [FromBody] LinkReconciliationItemRequest request)
    {
        _logger.LogInformation(
            "Iniciando vínculo de item com lançamento existente. ItemId: {ItemId}, LancamentoId: {LancamentoId}",
            id, request.TransactionId);

        try
        {
            var item = await _service.LinkItemAsync(id, request);

            if (item == null)
            {
                _logger.LogWarning("Item de conciliação não encontrado. ID: {ItemId}", id);
                return NotFound();
            }

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao vincular item. ItemId: {ItemId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Falha ao vincular item. ItemId: {ItemId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{id}/link")]
    public async Task<IActionResult> UnlinkItem(Guid id)
    {
        try
        {
            var item = await _service.UnlinkItemAsync(id);

            if (item == null)
                return NotFound();

            _logger.LogInformation("Vínculo removido. ItemId: {ItemId}", id);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        _logger.LogInformation("Iniciando confirmação da conciliação. SessaoId: {SessaoId}", id);

        try
        {
            var result = await _service.ConfirmAsync(id);

            _logger.LogInformation(
                "Conciliação confirmada. SessaoId: {SessaoId}, lançados: {Lancados}, divergência: {Divergencia}",
                id, result.PostedCount, result.Divergencia);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao confirmar conciliação. SessaoId: {SessaoId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/discard")]
    public async Task<IActionResult> Discard(Guid id)
    {
        _logger.LogInformation("Iniciando descarte da conciliação. SessaoId: {SessaoId}", id);

        try
        {
            var success = await _service.DiscardAsync(id);

            if (!success)
            {
                _logger.LogWarning("Tentativa de descartar conciliação inexistente. ID: {SessaoId}", id);
                return NotFound();
            }

            _logger.LogInformation("Conciliação descartada. SessaoId: {SessaoId}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao descartar conciliação. SessaoId: {SessaoId}, Erro: {Erro}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Lançamentos ignorados para sempre, para eventual consulta ou reversão.</summary>
    [HttpGet("ignored")]
    public async Task<IActionResult> GetIgnored()
    {
        var items = await _service.GetIgnoredItemsAsync();
        return Ok(items);
    }
}
