using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Models
{
    public class PedidoDetallesSpModel
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal Descuento { get; set; }
    }
}
