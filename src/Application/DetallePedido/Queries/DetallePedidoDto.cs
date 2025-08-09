using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DetallePedido.Queries
{
    public class DetallePedidoDto
    {
        public int DetalleId { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string? ProductoCodigo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public bool TieneIVA { get; set; }
        public bool TieneISC { get; set; }
        public decimal SubtotalLinea { get; set; }
        public decimal IVA { get; set; }
    }
}
