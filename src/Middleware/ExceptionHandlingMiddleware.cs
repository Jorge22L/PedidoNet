using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Application.Exceptions;
using Domain.Exceptions;

namespace Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status422UnprocessableEntity,
                    ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                await WriteResponseAsync(context,
                    StatusCodes.Status422UnprocessableEntity,
                    ex.Message);
            }
            catch (NotFoundException ex)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    ex.Message);
            }
            catch (ConflictException ex)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    ex.Message);
            }
            catch (Exception)
            {
                await WriteResponseAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Ocurrió un error interno.");
            }
        }

        private static async Task WriteResponseAsync(HttpContext context,
            int statusCode,
            string message,
            object? errors = null)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode,
                message,
                errors
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }

    
}
