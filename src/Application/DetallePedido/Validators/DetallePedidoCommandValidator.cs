using Application.DetallePedido.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DetallePedido.Validators
{
    public class DetallePedidoCommandValidator : AbstractValidator<DetallePedidoCommand>
    {
        public DetallePedidoCommandValidator()
        {
            RuleFor(x => x.ProductoId)
               .GreaterThan(0)
               .WithMessage(
                   "Debe especificar un producto válido.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0)
                .WithMessage(
                    "La cantidad debe ser mayor a cero.");

            RuleFor(x => x.Descuento)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    "El descuento no puede ser negativo.");
        }
    }
}
