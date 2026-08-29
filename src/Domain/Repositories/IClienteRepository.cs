using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IClienteRepository
    {
        Task<Cliente?> ObtenerPorIdAsync(int id);

        Task<List<Cliente>> ObtenerTodosAsync();

        Task<bool> ExisteAsync(int id);

        Task AgregarAsync(Cliente cliente);

        void Eliminar(Cliente cliente);
    }
}
