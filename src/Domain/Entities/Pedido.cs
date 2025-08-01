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
        public decimal SubTotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = "Contado"; // Valores válidos: Contado, Crédito, Transferencia, Tarjeta
        public string Estado { get; set; } = "Pendiente"; // Valores válidos: Pendiente, Completado, Cancelado

        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}
