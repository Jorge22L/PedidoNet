using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IPedidoRepository
    {
        Task<Pedido?> ObtenerPorIdAsync(
        int id,
        bool incluirDetalles = false,
        bool incluirRelaciones = false);

        Task<List<Pedido>> ObtenerTodosConRelacionesAsync();

        Task<List<Pedido>> ObtenerPorClienteConRelacionesAsync(
            int clienteId);

        Task AgregarAsync(Pedido pedido);

        void Eliminar(Pedido pedido);

        void EliminarDetalles(
            IEnumerable<DetallePedido> detalles);
    }
}
