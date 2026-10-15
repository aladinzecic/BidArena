namespace BidArena.Api.Middleware;

public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Dodaje <see cref="ExceptionHandlingMiddleware"/> u pipeline. Treba da bude što ranije
    /// (iza request logginga), da bi hvatao izuzetke svih middleware-a i kontrolera iza sebe.
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
