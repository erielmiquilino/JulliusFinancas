namespace Jullius.ServiceApi.Application.DTOs;

/// <summary>
/// Saldo consolidado do dashboard ("Em Conta"), acumulado a partir do marco zero.
/// </summary>
public class ConsolidatedBalanceDto
{
    /// <summary>Falso enquanto nenhuma conta tiver saldo de abertura; o dashboard mantém a fórmula antiga.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>Verdadeiro para meses anteriores ao marco zero, onde o acumulado não faz sentido.</summary>
    public bool IsHistoricalPeriod { get; set; }

    public DateTime? OpeningBalanceDate { get; set; }

    /// <summary>Σ aberturas + realizado do marco zero até o fim do mês exibido.</summary>
    public decimal EmConta { get; set; }

    /// <summary>Σ dos saldos lidos da Pluggy na última sincronização.</summary>
    public decimal SaldoBancos { get; set; }

    /// <summary>EmConta − SaldoBancos. Só é comparável no mês corrente ou posterior.</summary>
    public decimal? Divergencia { get; set; }

    public DateTime? SaldoBancosAtualizadoEm { get; set; }
    public List<ConsolidatedAccountBalanceDto> Contas { get; set; } = new();
}

public class ConsolidatedAccountBalanceDto
{
    public Guid BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public decimal LastKnownBalance { get; set; }
    public DateTime? LastBalanceSyncedAt { get; set; }

    /// <summary>Sinaliza uso de cheque especial.</summary>
    public bool IsNegative => LastKnownBalance < 0;
}
