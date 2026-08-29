using Domain.Constantes;
using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class DetallePedido
    {
        public int DetalleId { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public bool TieneIVA { get; set; }

        [NotMapped]
        public decimal ImporteBruto => Cantidad * PrecioUnitario;

        // Campos calculados
        [NotMapped]
        public decimal SubtotalLinea => ImporteBruto - Descuento;

        [NotMapped]
        public decimal IVA => TieneIVA ? SubtotalLinea * Impuestos.TasaIva : 0;

        [NotMapped]
        public decimal TotalLinea => SubtotalLinea + IVA;

        public void Validar()
        {
            if (ProductoId <= 0)
            {
                throw new DomainException(
                    "El producto del detalle no es válido.");
            }

            if (Cantidad <= 0)
            {
                throw new DomainException(
                    "La cantidad debe ser mayor que cero.");
            }

            if (PrecioUnitario < 0)
            {
                throw new DomainException(
                    "El precio unitario no puede ser negativo.");
            }

            if (Descuento < 0)
            {
                throw new DomainException(
                    "El descuento no puede ser negativo.");
            }

            if (Descuento > ImporteBruto)
            {
                throw new DomainException(
                    "El descuento del detalle no puede ser mayor " +
                    "que el importe de la línea.");
            }
        }
    }
}
