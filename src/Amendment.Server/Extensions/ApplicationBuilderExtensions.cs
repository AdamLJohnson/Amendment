using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using System.Text;
using Amendment.Shared;
using FluentValidation;

namespace Amendment.Server.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseFluentValidationExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(x =>
            {
                x.Run(async context =>
                {
                    var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = errorFeature.Error;

                    if (!(exception is ValidationException validationException))
                    {
                        throw exception;
                    }

                    var errors = validationException.Errors.Select(err => new ValidationErrorsResult
                    {
                        PropertyName = err.PropertyName,
                        ErrorMessage = err.ErrorMessage
                    });
                    var errorText = JsonSerializer.Serialize(errors);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(errorText, Encoding.UTF8);
                });
            });
        }
    }
}
