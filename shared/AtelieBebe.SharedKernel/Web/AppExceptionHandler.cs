using AtelieBebe.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.SharedKernel.Web;

/// <summary>Translates application/domain exceptions into consistent ProblemDetails responses. Shared by every microservice's Program.cs.</summary>
public sealed class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito"),
            UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            DomainException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro não tratado ao processar {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "Ocorreu um erro inesperado." : exception.Message,
            Instance = httpContext.Request.Path,
        }, cancellationToken);

        return true;
    }
}
