using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerActivoPorNombreAsync(string nombreUsuario);
        Task<Usuario?> ObtenerActivoPorEntraIdAsync(string entraObjectId);

        Task<Usuario?> ObtenerActivoPorEmailAsync(string email);
    }
}
