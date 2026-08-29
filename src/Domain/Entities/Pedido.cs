using Domain.Constantes;
using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Pedido
    {
        public int PedidoId { get; set; }
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public decimal SubTotal { get; private set; }
        public decimal IVA { get; private set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; private set; }
        public string FormaPago { get; set; } = "Contado"; // Valores válidos: Contado, Crédito, Transferencia, Tarjeta
        public string Estado { get; private set; } = EstadosPedido.Pendiente;

        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();

        public void AgregarDetalle(
       Producto producto,
       int cantidad,
       decimal descuento)
        {
            if (producto == null)
            {
                throw new DomainException(
                    "El producto es requerido.");
            }

            if (cantidad <= 0)
            {
                throw new DomainException(
                    "La cantidad debe ser mayor que cero.");
            }

            if (descuento < 0)
            {
                throw new DomainException(
                    "El descuento no puede ser negativo.");
            }

            var productoDuplicado =
                Detalles.Any(
                    d => d.ProductoId == producto.ProductoId);

            if (productoDuplicado)
            {
                throw new DomainException(
                    $"El producto '{producto.Nombre}' " +
                    "ya fue agregado al pedido.");
            }

            /*
             * El producto protege su propio stock.
             */
            producto.DescontarExistencia(cantidad);

            var detalle = new DetallePedido
            {
                ProductoId = producto.ProductoId,

                Cantidad = cantidad,

                /*
                 * Precio histórico tomado desde Producto.
                 */
                PrecioUnitario = producto.PrecioVenta,

                Descuento = descuento,

                /*
                 * Configuración fiscal tomada desde Producto.
                 */
                TieneIVA = producto.TieneIVA ?? false
            };

            detalle.Validar();

            Detalles.Add(detalle);
        }


        public void RecalcularTotales()
        {
            decimal subtotal = 0;
            decimal iva = 0;

            foreach (var detalle in Detalles)
            {
                detalle.Validar();

                subtotal += detalle.SubtotalLinea;
                iva += detalle.IVA;
            }

            var totalAntesDescuento =
                subtotal + iva;

            if (Descuento < 0)
            {
                throw new DomainException(
                    "El descuento general no puede ser negativo.");
            }

            if (Descuento > totalAntesDescuento)
            {
                throw new DomainException(
                    "El descuento general no puede ser mayor " +
                    "que el importe del pedido.");
            }

            SubTotal = subtotal;

            IVA = iva;

            Total =
                totalAntesDescuento - Descuento;
        }


        public void Completar()
        {
            if (Estado != EstadosPedido.Pendiente)
            {
                throw new DomainException(
                    $"No se puede completar un pedido " +
                    $"en estado '{Estado}'.");
            }

            Estado = EstadosPedido.Completado;
        }


        public void Cancelar()
        {
            if (Estado != EstadosPedido.Pendiente)
            {
                throw new DomainException(
                    $"No se puede cancelar un pedido " +
                    $"en estado '{Estado}'.");
            }

            Estado = EstadosPedido.Cancelado;
        }


        public void ValidarPuedeModificarse()
        {
            if (Estado != EstadosPedido.Pendiente)
            {
                throw new DomainException(
                    "Solo se pueden modificar pedidos pendientes.");
            }
        }


        public void ValidarPuedeEliminarse()
        {
            if (Estado != EstadosPedido.Pendiente)
            {
                throw new DomainException(
                    "Solo se pueden eliminar pedidos pendientes.");
            }
        }
    }
}
