using Application.Clientes.Commands;
using Application.DetallePedido.Commands;
using Application.DetallePedido.Validators;
using Application.Pedidos.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Pedidos.Validators
{
    public class CrearPedidoCommandValidator
    : AbstractValidator<CrearPedidoCommand>
    {
        public CrearPedidoCommandValidator()
        {
            RuleFor(x => x.ClienteId)
                .GreaterThan(0);

            RuleFor(x => x.FormaPago)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.Detalles)
                .NotNull()
                .NotEmpty();

            RuleForEach(x => x.Detalles)
                .SetValidator(
                    new DetallePedidoCommandValidator());

            RuleFor(x => x.Detalles)
                .Must(NoContieneDuplicados)
                .WithMessage(
                    "No se permite repetir un producto.");
        }

        private static bool NoContieneDuplicados(
            List<DetallePedidoCommand> detalles)
        {
            return detalles
                .GroupBy(x => x.ProductoId)
                .All(g => g.Count() == 1);
        }
    }
}
