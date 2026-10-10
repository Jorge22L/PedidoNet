using Application.Clientes.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Application.Clientes.Validators
{
    public class CrearClienteCommandValidator : AbstractValidator<CrearClienteCommand>
    {
        public CrearClienteCommandValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(100);
            ClienteUbicacionRules.Aplicar(this, x => x.Latitud, x => x.Longitud);
        }
    }
}
