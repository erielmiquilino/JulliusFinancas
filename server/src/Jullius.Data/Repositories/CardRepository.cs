using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Jullius.Data.Repositories;

public class CardRepository : ICardRepository
{
    private readonly JulliusDbContext _context;
    private readonly ILogger<CardRepository> _logger;

    public CardRepository(JulliusDbContext context, ILogger<CardRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Card> CreateAsync(Card card)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Iniciando criação de cartão no repositório. Nome: {Nome}, Banco: {Banco}",
            card.Name, card.IssuingBank);

        try
        {
            await _context.Set<Card>().AddAsync(card);
            await _context.SaveChangesAsync();
            
            stopwatch.Stop();
            _logger.LogInformation("Cartão criado com sucesso no repositório. " +
                "ID: {CartaoId}, Nome: {Nome}, Tempo: {TempoMs}ms",
                card.Id, card.Name, stopwatch.ElapsedMilliseconds);
            
            return card;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao criar cartão no repositório. " +
                "Nome: {Nome}, Banco: {Banco}, Tempo: {TempoMs}ms",
                card.Name, card.IssuingBank, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Iniciando exclusão de cartão no repositório. ID: {CartaoId}", id);

        try
        {
            var card = await GetByIdAsync(id);
            if (card == null)
            {
                _logger.LogWarning("Tentativa de excluir cartão inexistente no repositório. ID: {CartaoId}", id);
                return;
            }

            _context.Set<Card>().Remove(card);
            await _context.SaveChangesAsync();
            
            stopwatch.Stop();
            _logger.LogInformation("Cartão excluído com sucesso no repositório. " +
                "ID: {CartaoId}, Tempo: {TempoMs}ms", id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao excluir cartão no repositório. " +
                "ID: {CartaoId}, Tempo: {TempoMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<Card>> GetAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Iniciando busca de todos os cartões no repositório");

        try
        {
            var cards = await _context.Set<Card>().ToListAsync();
            
            stopwatch.Stop();
            _logger.LogInformation("Busca de cartões concluída no repositório. " +
                "Total encontrado: {TotalCartoes}, Tempo: {TempoMs}ms",
                cards.Count, stopwatch.ElapsedMilliseconds);
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Query lenta detectada na busca de todos os cartões. " +
                    "Tempo: {TempoMs}ms", stopwatch.ElapsedMilliseconds);
            }
            
            return cards;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao buscar todos os cartões no repositório. " +
                "Tempo: {TempoMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Card?> GetByIdAsync(Guid id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Buscando cartão por ID no repositório: {CartaoId}", id);

        try
        {
            var card = await _context.Set<Card>().FindAsync(id);
            
            stopwatch.Stop();
            
            if (card == null)
            {
                _logger.LogDebug("Cartão não encontrado no repositório. ID: {CartaoId}, Tempo: {TempoMs}ms",
                    id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug("Cartão encontrado no repositório. " +
                    "ID: {CartaoId}, Nome: {Nome}, Tempo: {TempoMs}ms",
                    card.Id, card.Name, stopwatch.ElapsedMilliseconds);
            }
            
            return card;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao buscar cartão por ID no repositório. " +
                "ID: {CartaoId}, Tempo: {TempoMs}ms", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpdateAsync(Card card)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Iniciando atualização de cartão no repositório. " +
            "ID: {CartaoId}, Nome: {Nome}", card.Id, card.Name);

        try
        {
            _context.Set<Card>().Update(card);
            await _context.SaveChangesAsync();
            
            stopwatch.Stop();
            _logger.LogInformation("Cartão atualizado com sucesso no repositório. " +
                "ID: {CartaoId}, Nome: {Nome}, Tempo: {TempoMs}ms",
                card.Id, card.Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Erro ao atualizar cartão no repositório. " +
                "ID: {CartaoId}, Nome: {Nome}, Tempo: {TempoMs}ms",
                card.Id, card.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
} 