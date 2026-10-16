using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Itinerario.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null)
            {
                continue;
            }

            var tipoValidator = typeof(IValidator<>).MakeGenericType(argumento.GetType());

            if (_serviceProvider.GetService(tipoValidator) is not IValidator validator)
            {
                continue;
            }

            var tipoContexto = typeof(ValidationContext<>).MakeGenericType(argumento.GetType());
            var contexto = (IValidationContext)Activator.CreateInstance(tipoContexto, argumento)!;

            var resultado = await validator.ValidateAsync(contexto, context.HttpContext.RequestAborted);

            foreach (var error in resultado.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return;
        }

        await next();
    }
}
