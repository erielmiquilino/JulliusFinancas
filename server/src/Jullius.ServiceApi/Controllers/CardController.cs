using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Application.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardController : ODataController
{
    private readonly CardService _service;

    public CardController(CardService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCardRequest request)
    {
        try
        {
            var card = await _service.CreateCardAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
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
        var cards = await _service.GetAllCardsAsync();
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var card = await _service.GetCardByIdAsync(id);
        if (card == null)
            return NotFound();
            
        return Ok(card);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteCardAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCardRequest request)
    {
        try
        {
            var card = await _service.UpdateCardAsync(id, request);
            if (card == null)
                return NotFound();
                
            return Ok(card);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
} 