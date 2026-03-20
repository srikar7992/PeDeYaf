using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FluentValidation;
using PeDeYaf.Domain.Exceptions;

namespace PeDeYaf.Api.Middleware;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title, detail) = ex switch
        {
            ValidationException vex => (StatusCodes.Status400BadRequest, "Validation Error", vex.Errors.Select(e => e.ErrorMessage).First()),
            DocumentNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "The requested document was not found."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", "You do not have permission to perform this action."),
            TooManyRequestsException => (StatusCodes.Status429TooManyRequests, "Too Many Requests", "Rate limit exceeded. Try again later."),
            InvalidOtpException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid or expired OTP."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // Add traceId for support requests mapping back to Serilog logs
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
