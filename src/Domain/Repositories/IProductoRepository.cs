using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IProductoRepository
    {
        Task<Producto?> ObtenerPorIdAsync(int id);

        Task<Producto?> ObtenerPorClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<List<Producto>> ObtenerPorIdsAsync(IEnumerable<int> ids);

        Task<List<Producto>> ObtenerTodosAsync();

        Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default);

        void Eliminar(Producto producto);
    }
}
