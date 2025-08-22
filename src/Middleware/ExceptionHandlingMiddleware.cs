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
            catch (Exception e)
            {
                await HandleExceptionAsync(context, e);
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
    }

    
}
