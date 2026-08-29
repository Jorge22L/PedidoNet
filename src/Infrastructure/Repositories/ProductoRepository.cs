using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(Producto producto)
        {
            await _context.Productos.AddAsync(producto);
        }

        public void Eliminar(Producto producto)
        {
            _context.Productos.Remove(producto);
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == id);
        }

        public async Task<List<Producto>> ObtenerPorIdsAsync(IEnumerable<int> ids)
        {
            var idsLista = ids
                .Distinct()
                .ToList();

            return await _context.Productos
                .Where(p => idsLista.Contains(p.ProductoId))
                .ToListAsync();
        }

        public async Task<List<Producto>> ObtenerTodosAsync()
        {
            return await _context.Productos
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
