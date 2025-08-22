using Application.Pedidos.Commands;
using Application.Pedidos.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPedidoService
    {
        Task<List<PedidoDto>> ObtenerTodosAsync();
        Task<PedidoDto>? ObtenerPorIdAsync(int id);
        Task<List<PedidoDto>> ObtenerPorClienteAsync(int clienteId);
        Task<int> CrearPedidoAsync(CrearPedidoCommand command);
        Task<bool> ActualizarPedidoAsync(int id, ActualizarPedidoCommand command);
        Task<bool> EliminarPedidoAsync(int id);
        Task<bool> CambiarEstadoPedidoAsync(int id, string nuevoEstado);
        Task<bool> CompletarPedidoAsync(int id);
        Task<bool> CancelarPedidoAsync(int id);
    }
}
