using Application.Pedidos.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IPedidoWriteRepository
    {
        Task<int> GuardarAsync(CrearPedidoCommand command, CancellationToken cancellationToken = default);
        Task<bool> ActualizarAsync(int pedidoId, ActualizarPedidoCommand command, CancellationToken cancellationToken = default);
        Task<bool> EliminarAsync(int pedidoId, CancellationToken cancellationToken = default);
        Task<bool> CompletarAsync(int pedidoId, CancellationToken cancellationToken = default);
        Task<bool> CancelarAsync(int pedidoId, CancellationToken cancellationToken = default);
    }
}
