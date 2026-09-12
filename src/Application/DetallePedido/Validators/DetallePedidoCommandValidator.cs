using Application.DetallePedido.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DetallePedido.Validators
{
    public class DetallePedidoCommandValidator
    : AbstractValidator<DetallePedidoCommand>
    {
        public DetallePedidoCommandValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0);

            RuleFor(x => x.Cantidad)
                .GreaterThan(0);

            RuleFor(x => x.Descuento)
                .GreaterThanOrEqualTo(0);
        }
    }
}
