using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jullius.ServiceApi.Application.Services;

namespace Jullius.ServiceApi.Integrations.Pluggy;

/// <summary>
/// Cliente da API da Pluggy (Open Finance).
///
/// Dois detalhes do contrato real divergem da documentação pública e estão codificados aqui:
/// o endpoint v1 /transactions foi desativado (HTTP 410) em favor de /v2/transactions com
/// paginação por cursor, e o v2 aceita "dateFrom"/"dateTo" — não "from"/"to" — e rejeita
/// "pageSize"/"page" com HTTP 400.
/// </summary>
public class PluggyClient
{
    public const string HttpClientName = "PluggyApi";
    public const string ClientIdConfigKey = "PluggyClientId";
    public const string ClientSecretConfigKey = "PluggyClientSecret";

    private const string BaseUrl = "https://api.pluggy.ai";
    private const int MaxPages = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BotConfigurationService _configService;
    private readonly PluggyApiKeyCache _apiKeyCache;
    private readonly ILogger<PluggyClient> _logger;

    public PluggyClient(
        IHttpClientFactory httpClientFactory,
        BotConfigurationService configService,
        PluggyApiKeyCache apiKeyCache,
        ILogger<PluggyClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _apiKeyCache = apiKeyCache;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var clientId = await _configService.GetDecryptedValueAsync(ClientIdConfigKey);
        var clientSecret = await _configService.GetDecryptedValueAsync(ClientSecretConfigKey);
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    /// <summary>Valida se a conexão continua viva. Lança <see cref="PluggyItemNotFoundException"/> em 404.</summary>
    public async Task<PluggyItem> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/items/{itemId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new PluggyItemNotFoundException(itemId);

        await EnsureSuccessAsync(response, cancellationToken);

        var item = await response.Content.ReadFromJsonAsync<PluggyItem>(JsonOptions, cancellationToken);
        return item ?? throw new InvalidOperationException($"Resposta vazia ao consultar o item {itemId} na Pluggy.");
    }

    public async Task<IReadOnlyList<PluggyAccount>> GetAccountsAsync(string itemId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/accounts?itemId={itemId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var page = await response.Content.ReadFromJsonAsync<PluggyAccountList>(JsonOptions, cancellationToken);
        return page?.Results ?? new List<PluggyAccount>();
    }

    public async Task<PluggyAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/accounts/{accountId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PluggyAccount>(JsonOptions, cancellationToken);
    }

    /// <summary>
    /// Percorre todas as páginas de /v2/transactions seguindo o cursor "next".
    /// </summary>
    public async Task<IReadOnlyList<PluggyTransaction>> GetTransactionsAsync(
        string accountId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        var transactions = new List<PluggyTransaction>();
        var path = $"/v2/transactions?accountId={accountId}" +
                   $"&dateFrom={dateFrom:yyyy-MM-dd}" +
                   $"&dateTo={dateTo:yyyy-MM-dd}";

        for (var page = 0; page < MaxPages && path is not null; page++)
        {
            using var response = await SendAsync(HttpMethod.Get, path, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var body = await response.Content.ReadFromJsonAsync<PluggyTransactionPage>(JsonOptions, cancellationToken);
            if (body is null)
                break;

            transactions.AddRange(body.Results);
            path = NormalizeCursor(body.Next);
        }

        return transactions;
    }

    private static string? NormalizeCursor(string? next)
    {
        if (string.IsNullOrWhiteSpace(next))
            return null;

        if (next.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return next;

        // A Pluggy devolve o cursor tanto como "?accountId=..." quanto como "/v2/transactions?...".
        return next.StartsWith('?')
            ? $"/v2/transactions{next}"
            : next.StartsWith('/') ? next : $"/{next}";
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var apiKey = await GetApiKeyAsync(cancellationToken);
        var response = await SendWithKeyAsync(method, path, apiKey, cancellationToken);

        // A chave tem validade de 2h; se expirar antes do cache, renova uma vez e repete.
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            _apiKeyCache.Invalidate();
            apiKey = await GetApiKeyAsync(cancellationToken);
            response = await SendWithKeyAsync(method, path, apiKey, cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithKeyAsync(
        HttpMethod method,
        string path,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, BuildUri(path));
        request.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        var cached = _apiKeyCache.Get();
        if (cached is not null)
            return cached;

        var clientId = await _configService.GetDecryptedValueAsync(ClientIdConfigKey);
        var clientSecret = await _configService.GetDecryptedValueAsync(ClientSecretConfigKey);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
                "Credenciais da Pluggy não configuradas. Cadastre PluggyClientId e PluggyClientSecret em Configurações.");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsJsonAsync(
            BuildUri("/auth"),
            new { clientId, clientSecret },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Falha ao autenticar na Pluggy. Status: {StatusCode}",
                (int)response.StatusCode);
            throw new InvalidOperationException(
                "Não foi possível autenticar na Pluggy. Verifique o clientId e o clientSecret cadastrados.");
        }

        var auth = await response.Content.ReadFromJsonAsync<PluggyAuthResponse>(JsonOptions, cancellationToken);
        if (string.IsNullOrWhiteSpace(auth?.ApiKey))
            throw new InvalidOperationException("A Pluggy não devolveu uma apiKey válida.");

        _apiKeyCache.Set(auth.ApiKey);
        _logger.LogInformation("Autenticação na Pluggy renovada com sucesso.");

        return auth.ApiKey;
    }

    private static Uri BuildUri(string path)
    {
        return path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? new Uri(path, UriKind.Absolute)
            : new Uri($"{BaseUrl}{path}", UriKind.Absolute);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var truncated = body.Length > 500 ? body[..500] : body;

        _logger.LogError(
            "Chamada à Pluggy falhou. Status: {StatusCode}. Corpo: {Body}",
            (int)response.StatusCode,
            truncated);

        throw new InvalidOperationException(
            $"A Pluggy respondeu {(int)response.StatusCode}. Detalhe: {truncated}");
    }
}
