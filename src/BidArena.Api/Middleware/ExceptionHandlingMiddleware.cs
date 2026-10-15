using System.Data.Common;
using BidArena.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Api.Middleware;

/// <summary>
/// Globalni middleware za obradu grešaka. Okružuje ostatak pipeline-a u try/catch i svaki neobrađen
/// izuzetak pretvara u RFC 7807 Problem Details odgovor (400/401/403/404/409/500).
/// Registruje se preko <see cref="ExceptionHandlingMiddlewareExtensions.UseExceptionHandling"/>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    // Nestandardni kod (nginx konvencija): klijent je prekinuo vezu pre nego što je dobio odgovor.
    private const int ClientClosedRequest = 499;

    private const string ProblemJson = "application/problem+json";

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Klijent je zatvorio vezu: nije greška servera i nema kome da se pošalje odgovor.
            _logger.LogInformation(
                "Klijent je prekinuo zahtev {Method} {Path}", context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = ClientClosedRequest;
        }
        catch (Exception exception)
        {
            // Kad je odgovor već počeo da se šalje, status i telo se više ne mogu menjati.
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception,
                    "Izuzetak nakon početka slanja odgovora ({Method} {Path}); Problem Details se ne može poslati",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problem;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Validacija nije prošla: {@Errors}", errors);

            problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validacija nije prošla"
            };
        }
        else
        {
            var (status, title) = Map(exception);

            if (status >= 500)
                _logger.LogError(exception, "Neobrađen izuzetak");
            else
                _logger.LogWarning("{Title}: {Message}", title, exception.Message);

            problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Za 5xx ne otkrivamo detalje klijentu.
                Detail = status >= 500 ? null : exception.Message
            };
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // Prosleđuje se stvarni tip: ValidationProblemDetails mora da zadrži polje "errors".
        await context.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: ProblemJson, cancellationToken: context.RequestAborted);
    }

    private static (int Status, string Title) Map(Exception exception) => exception switch
    {
        // Neispravan HTTP zahtev (npr. prevelik body): Kestrel već nosi odgovarajući status.
        BadHttpRequestException bad => (bad.StatusCode, "Neispravan zahtev"),
        // Baza nedostupna / istekao timeout: EF ovo umota u InvalidOperationException, pa mora pre nje,
        // inače bi pad baze izgledao kao poslovni konflikt (409).
        _ when IsTransientFailure(exception) => (StatusCodes.Status503ServiceUnavailable, "Servis privremeno nije dostupan, pokušajte ponovo"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Neispravan zahtev"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Neuspešna prijava"),
        ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Pristup zabranjen"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resurs nije pronađen"),
        InvalidOperationException => (StatusCodes.Status409Conflict, "Operacija nije dozvoljena"),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Konflikt pri istovremenoj izmeni, pokušajte ponovo"),
        _ => (StatusCodes.Status500InternalServerError, "Neočekivana greška na serveru")
    };

    // Prolazni kvar infrastrukture negde u lancu izuzetaka (npr. Npgsql: veza odbijena, timeout).
    // Trajne greške baze (npr. duplikat ključa) nisu prolazne i ostaju 500.
    private static bool IsTransientFailure(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is TimeoutException or DbException { IsTransient: true })
                return true;
        }

        return false;
    }
}
