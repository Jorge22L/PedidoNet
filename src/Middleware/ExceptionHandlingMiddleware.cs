using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Application;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            object errorResponse;

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = HttpStatusCode.BadRequest;
                    errorResponse = new { message = validationEx.Message, errors = validationEx.Errors };
                    break;

                case NotFoundException notFoundEx:
                    statusCode = HttpStatusCode.NotFound;
                    errorResponse = new { message = notFoundEx.Message };
                    break;

                default:
                    errorResponse = new { message = exception.Message };
                    break;
;
            }

            response.StatusCode = (int)statusCode;
            var json = JsonSerializer.Serialize(errorResponse);
            return response.WriteAsync(json);
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
