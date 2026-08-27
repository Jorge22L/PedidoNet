using Application.DetallePedido.Commands;
using Application.DetallePedido.Validators;
using Application.Pedidos.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Pedidos.Validators
{
    public class ActualizarPedidoCommandValidator : AbstractValidator<ActualizarPedidoCommand>
    {
        private static readonly string[] FormasPagoValidas =
        {
            "Contado",
            "Crédito",
            "Transferencia",
            "Tarjeta"
        };

        public ActualizarPedidoCommandValidator()
        {
            RuleFor(x => x.ClienteId)
               .GreaterThan(0)
               .When(x => x.ClienteId != 0)
               .WithMessage(
                   "El cliente debe ser válido.");

            RuleFor(x => x.Descuento)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Descuento.HasValue)
                .WithMessage(
                    "El descuento no puede ser negativo.");

            RuleFor(x => x.FormaPago)
                .Must(forma =>
                    forma == null ||
                    FormasPagoValidas.Contains(
                        forma,
                        StringComparer.OrdinalIgnoreCase))
                .WithMessage(
                    "La forma de pago debe ser Contado, Crédito, " +
                    "Transferencia o Tarjeta.");

            RuleForEach(x => x.Detalles)
                .SetValidator(new DetallePedidoCommandValidator());

            RuleFor(x => x.Detalles)
                .Must(NoContieneProductosDuplicados)
                .When(x => x.Detalles != null)
                .WithMessage(
                    "No se permite repetir un producto dentro del pedido.");
        }

        private static bool NoContieneProductosDuplicados(List<DetallePedidoCommand>? detalles)
        {
            if(detalles == null)
            {
                return true;
            }

            return detalles
                .GroupBy(x => x.ProductoId)
                .All(g => g.Count() == 1);
        }
    }
}
