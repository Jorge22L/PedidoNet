using Application.DetallePedido.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pedidos.Commands
{
    public class ActualizarPedidoCommand
    {
        public int ClienteId { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal? Descuento { get; set; }
        [StringLength(20)]
        public string? FormaPago { get; set; }
        [StringLength(20)]
        public string? Estado {  get; set; } // Pendiente, Completado, Cancelado
        public List<DetallePedidoCommand>? Detalles { get; set; }
    }
}
