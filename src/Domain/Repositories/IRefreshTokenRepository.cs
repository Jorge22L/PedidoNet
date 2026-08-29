using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> ObtenerPorHashAsync(
        string tokenHash,
        bool incluirUsuario = false);

        Task AgregarAsync(RefreshToken refreshToken);
    }
}
