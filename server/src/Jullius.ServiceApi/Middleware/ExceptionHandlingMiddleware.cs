using Serilog;
using System.Net;
using System.Text.Json;

namespace Jullius.ServiceApi.Middleware;

/// <summary>
/// Middleware para captura e tratamento global de exceções com logging
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        
        _logger.LogError(exception, 
            "Erro não tratado capturado para {RequestPath} {RequestMethod}. " +
            "CorrelationId: {CorrelationId}. Tipo da exceção: {ExceptionType}. " +
            "Mensagem: {ExceptionMessage}",
            context.Request.Path,
            context.Request.Method,
            correlationId,
            exception.GetType().Name,
            exception.Message);

        var response = context.Response;
        
        // Only set content type and status code if response hasn't started
        if (!response.HasStarted)
        {
            response.ContentType = "application/json";

            var (statusCode, message) = GetErrorResponse(exception);
            response.StatusCode = (int)statusCode;

            var errorResponse = new
            {
                erro = message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation(
                "Resposta de erro enviada para {RequestPath}. " +
                "Status: {StatusCode}. CorrelationId: {CorrelationId}",
                context.Request.Path,
                statusCode,
                correlationId);

            await response.WriteAsync(jsonResponse);
        }
        else
        {
            // Response has already started, just log the error
            _logger.LogWarning(
                "Não foi possível enviar resposta de erro para {RequestPath} - resposta já foi iniciada. " +
                "CorrelationId: {CorrelationId}",
                context.Request.Path,
                correlationId);
        }
    }

    private static (HttpStatusCode statusCode, string message) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Parâmetro obrigatório não fornecido"),
            ArgumentException => (HttpStatusCode.BadRequest, "Parâmetros inválidos fornecidos"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Funcionalidade não implementada"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Tempo limite da operação excedido"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Operação inválida para o estado atual"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado"),
            _ => (HttpStatusCode.InternalServerError, "Erro interno do servidor")
        };
    }
}
