using Application.DetallePedido.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pedidos.Queries
{
    public class PedidoDto
    {
        public int PedidoId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal SubTotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public List<DetallePedidoDto> Detalles { get; set; } = new List<DetallePedidoDto>();
    }

}
