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
public class CardController : ODataController
{
    private readonly CardService _service;
    private readonly ILogger<CardController> _logger;

    public CardController(CardService service, ILogger<CardController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCardRequest request)
    {
        _logger.LogInformation("Iniciando criação de cartão. Dados: {Nome} - {Banco}", 
            request.Name, request.IssuingBank);

        try
        {
            var card = await _service.CreateCardAsync(request);
            
            _logger.LogInformation("Cartão criado com sucesso. Id: {CartaoId}, Nome: {Nome}, Banco: {Banco}", 
                card.Id, card.Name, card.IssuingBank);
                
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na criação do cartão devido a dados inválidos. Erro: {Erro}. Request: {@Request}", 
                ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todos os cartões");

        var cards = await _service.GetAllCardsAsync();
        
        _logger.LogInformation("Busca de cartões concluída. Total encontrado: {TotalCartoes}", cards.Count());
        
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Iniciando busca de cartão por ID: {CartaoId}", id);

        var card = await _service.GetCardByIdAsync(id);
        
        if (card == null)
        {
            _logger.LogWarning("Cartão não encontrado para ID: {CartaoId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Cartão encontrado com sucesso. ID: {CartaoId}, Nome: {Nome}", 
            card.Id, card.Name);
            
        return Ok(card);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de cartão. ID: {CartaoId}", id);

        var result = await _service.DeleteCardAsync(id);
        
        if (!result)
        {
            _logger.LogWarning("Tentativa de excluir cartão inexistente. ID: {CartaoId}", id);
            return NotFound();
        }

        _logger.LogInformation("Cartão excluído com sucesso. ID: {CartaoId}", id);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCardRequest request)
    {
        _logger.LogInformation("Iniciando atualização de cartão. ID: {CartaoId}, Dados: {@Request}", 
            id, request);

        try
        {
            var card = await _service.UpdateCardAsync(id, request);
            
            if (card == null)
            {
                _logger.LogWarning("Tentativa de atualizar cartão inexistente. ID: {CartaoId}", id);
                return NotFound();
            }
                
            _logger.LogInformation("Cartão atualizado com sucesso. ID: {CartaoId}, Nome: {Nome}", 
                card.Id, card.Name);
                
            return Ok(card);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização do cartão devido a dados inválidos. ID: {CartaoId}, Erro: {Erro}. Request: {@Request}", 
                id, ex.Message, request);
            return BadRequest(ex.Message);
        }
    }
} 