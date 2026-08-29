using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task<RefreshToken?> ObtenerPorHashAsync(string tokenHash, bool incluirUsuario = false)
        {
            IQueryable<RefreshToken> query = _context.RefreshTokens;
            if (incluirUsuario)
            {
                query = query
                    .Include(x => x.Usuario);
            }

            return await query.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }
    }
}
