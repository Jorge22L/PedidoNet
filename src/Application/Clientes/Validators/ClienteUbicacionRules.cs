using Application.Clientes.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Application.Clientes.Validators
{
    public class ClienteUbicacionRules
    {
        public static void Aplicar<T>(AbstractValidator<T> validator, 
            Expression<Func<T, decimal?>> latitud, Expression<Func<T, decimal?>> longitud)
        {
            var obtenerLatitud = latitud.Compile();
            var obtenerLongitud = longitud.Compile();

            validator.RuleFor(latitud)
                .InclusiveBetween(-90m, 90m)
                .When(x => obtenerLatitud(x).HasValue)
                .WithMessage("La latitud debe estar entre -90 y 90.");

            validator.RuleFor(longitud)
                .InclusiveBetween(-180m, 180m)
                .When(x => obtenerLongitud(x).HasValue)
                .WithMessage("La latitud debe estar entre -180 y 180.");

            // ambas o ninguna
            validator.RuleFor(x => x)
                .Must(x => obtenerLatitud(x).HasValue == obtenerLongitud(x).HasValue)
                .WithName("Ubicacion")
                .WithMessage("La latitud y la longitud deben enviarse juntas");
        }
    }
}
