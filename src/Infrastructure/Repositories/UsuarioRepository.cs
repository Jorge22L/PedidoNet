using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;

        public UsuarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerActivoPorEmailAsync(string email)
        {
            var normalizado = email.Trim().ToLower();

            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizado && u.Activo);
        }

        public async Task<Usuario?> ObtenerActivoPorEntraIdAsync(string entraObjectId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId && u.Activo);
        }

        public async Task<Usuario?> ObtenerActivoPorNombreAsync(string nombreUsuario)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.Activo);
        }
    }
}
