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
    public class CrearPedidoCommandValidator : AbstractValidator<CrearPedidoCommand>
    {
        private static readonly string[] FormasPagoValidas =
        {
            "Contado",
            "Crédito",
            "Transferencia",
            "Tarjeta"
        };

        public CrearPedidoCommandValidator()
        {
            

            RuleFor(x => x.Fecha)
                .NotEmpty()
                .WithMessage("La fecha es requerida.");

            RuleFor(x => x.Descuento)
                .GreaterThanOrEqualTo(0)
                .WithMessage(
                    "El descuento del pedido no puede ser negativo.");

            RuleFor(x => x.FormaPago)
                .NotEmpty()
                .WithMessage("La forma de pago es requerida.")
                .Must(forma =>
                    FormasPagoValidas.Contains(
                        forma,
                        StringComparer.OrdinalIgnoreCase))
                .WithMessage(
                    "La forma de pago debe ser Contado, Crédito, " +
                    "Transferencia o Tarjeta.");

            RuleFor(x => x.Detalles)
                .NotNull()
                .WithMessage(
                    "El pedido debe contener detalles.")
                .NotEmpty()
                .WithMessage(
                    "El pedido debe contener al menos un producto.");

            RuleForEach(x => x.Detalles)
                .SetValidator(new DetallePedidoCommandValidator());

            RuleFor(x => x.Detalles)
                .Must(NoContieneProductosDuplicados)
                .WithMessage(
                    "No se permite repetir un producto dentro del pedido.");
        }

        private static bool NoContieneProductosDuplicados(List<DetallePedidoCommand> detalles)
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
