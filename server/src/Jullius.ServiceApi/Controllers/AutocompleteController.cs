using Microsoft.AspNetCore.Mvc;
using Jullius.ServiceApi.Application.Services;

using Microsoft.AspNetCore.Authorization;

namespace Jullius.ServiceApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AutocompleteController : ControllerBase
{
    private readonly AutocompleteService _service;
    private readonly ILogger<AutocompleteController> _logger;

    public AutocompleteController(AutocompleteService service, ILogger<AutocompleteController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("descriptions")]
    public async Task<IActionResult> GetDescriptionSuggestions([FromQuery] string search)
    {
        _logger.LogDebug("Buscando sugestões de descrição para o termo: {SearchTerm}", search);

        if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
        {
            return Ok(Array.Empty<string>());
        }

        var suggestions = await _service.GetDescriptionSuggestionsAsync(search);
        
        _logger.LogDebug("Encontradas {Count} sugestões para o termo: {SearchTerm}", 
            suggestions.Count(), search);
        
        return Ok(suggestions);
    }
}

