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
public class CategoryController : ODataController
{
    private readonly CategoryService _service;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(CategoryService service, ILogger<CategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        _logger.LogInformation("Iniciando criação de categoria. Nome: {Nome}", request.Name);

        try
        {
            var category = await _service.CreateCategoryAsync(request);
            
            _logger.LogInformation("Categoria criada com sucesso. Id: {CategoriaId}, Nome: {Nome}", 
                category.Id, category.Name);
                
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na criação da categoria. Erro: {Erro}. Request: {@Request}", 
                ex.Message, request);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [EnableQuery(MaxTop = 1000)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Iniciando busca de todas as categorias");

        var categories = await _service.GetAllCategoriesAsync();
        
        _logger.LogInformation("Busca de categorias concluída. Total encontrado: {TotalCategorias}", 
            categories.Count());
        
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Iniciando busca de categoria por ID: {CategoriaId}", id);

        var category = await _service.GetCategoryByIdAsync(id);
        
        if (category == null)
        {
            _logger.LogWarning("Categoria não encontrada para ID: {CategoriaId}", id);
            return NotFound();
        }
            
        _logger.LogInformation("Categoria encontrada. ID: {CategoriaId}, Nome: {Nome}", 
            category.Id, category.Name);
            
        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Iniciando exclusão de categoria. ID: {CategoriaId}", id);

        var (success, errorMessage) = await _service.DeleteCategoryAsync(id);
        
        if (!success)
        {
            if (errorMessage == "Categoria não encontrada")
            {
                _logger.LogWarning("Tentativa de excluir categoria inexistente. ID: {CategoriaId}", id);
                return NotFound();
            }
            
            _logger.LogWarning("Não foi possível excluir categoria. ID: {CategoriaId}, Motivo: {Motivo}", 
                id, errorMessage);
            return BadRequest(errorMessage);
        }

        _logger.LogInformation("Categoria excluída com sucesso. ID: {CategoriaId}", id);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        _logger.LogInformation("Iniciando atualização de categoria. ID: {CategoriaId}, Dados: {@Request}", 
            id, request);

        try
        {
            var category = await _service.UpdateCategoryAsync(id, request);
            
            if (category == null)
            {
                _logger.LogWarning("Tentativa de atualizar categoria inexistente. ID: {CategoriaId}", id);
                return NotFound();
            }
                
            _logger.LogInformation("Categoria atualizada com sucesso. ID: {CategoriaId}, Nome: {Nome}", 
                category.Id, category.Name);
                
            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha na atualização da categoria. ID: {CategoriaId}, Erro: {Erro}. Request: {@Request}", 
                id, ex.Message, request);
            return BadRequest(ex.Message);
        }
    }
}

