using System.ComponentModel;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Microsoft.SemanticKernel;

namespace Jullius.ServiceApi.Telegram.Plugins;

/// <summary>
/// Plugin SK para gerenciamento de categorias financeiras.
/// </summary>
public sealed class CategoryPlugin
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoryPlugin> _logger;

    public CategoryPlugin(
        CategoryService categoryService,
        ILogger<CategoryPlugin> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [KernelFunction("ListCategories")]
    [Description("Lista todas as categorias financeiras cadastradas. Use quando o usuário pedir para ver categorias ou quando precisar sugerir categorias.")]
    public async Task<string> ListCategoriesAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var categoryList = categories.ToList();

            if (categoryList.Count == 0)
                return "📂 Nenhuma categoria cadastrada.";

            var names = string.Join(", ", categoryList.Select(c => c.Name));
            return $"📂 Categorias disponíveis: {names}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar categorias via Telegram SK");
            return $"❌ Erro ao listar categorias: {ex.Message}";
        }
    }

    [KernelFunction("CreateCategory")]
    [Description("Cria uma nova categoria financeira. Use apenas quando o usuário pedir explicitamente para criar uma categoria.")]
    public async Task<string> CreateCategoryAsync(
        [Description("Nome da categoria (ex: 'Alimentação', 'Saúde', 'Educação')")] string name,
        [Description("Cor em hexadecimal (ex: '#FF5722'). Se não informada, usa cor padrão.")] string color = "#607D8B")
    {
        try
        {
            var request = new CreateCategoryRequest
            {
                Name = name,
                Color = color
            };

            var created = await _categoryService.CreateCategoryAsync(request);
            return $"✅ Categoria \"{created.Name}\" criada com sucesso!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar categoria via Telegram SK");
            return $"❌ Erro ao criar a categoria: {ex.Message}";
        }
    }

    [KernelFunction("DeleteCategory")]
    [Description("Remove uma categoria financeira pelo nome. A categoria não pode estar em uso por transações.")]
    public async Task<string> DeleteCategoryAsync(
        [Description("Nome da categoria a ser removida")] string name)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var category = categories.FirstOrDefault(c =>
                c.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return $"❌ Categoria \"{name}\" não encontrada.";

            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(category.Id);

            if (!success)
                return $"❌ Não foi possível remover a categoria: {errorMessage}";

            return $"✅ Categoria \"{name}\" removida com sucesso!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover categoria via Telegram SK");
            return $"❌ Erro ao remover a categoria: {ex.Message}";
        }
    }
}
