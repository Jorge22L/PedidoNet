using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly ApplicationDbContext _context;

        public PedidoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(Pedido pedido)
        {
            await _context.Pedidos.AddAsync(pedido);
        }

        public void Eliminar(Pedido pedido)
        {
            _context.Pedidos.Remove(pedido);
        }

        public void EliminarDetalles(IEnumerable<DetallePedido> detalles)
        {
            _context.DetallePedidos.RemoveRange(detalles);
        }

        public async Task<List<Pedido>> ObtenerPorClienteConRelacionesAsync(int clienteId)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.ClienteId == clienteId)
                .ToListAsync();

        }

        public async Task<Pedido?> ObtenerPorIdAsync(int id, bool incluirDetalles = false, bool incluirRelaciones = false)
        {
            IQueryable<Pedido> query = _context.Pedidos;

            if (incluirRelaciones)
            {
                query = query
                    .Include(p => p.Cliente)
                    .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto);
            }
            else if (incluirDetalles)
            {
                query = query
                    .Include(p => p.Detalles);
            }

            return await query.FirstOrDefaultAsync(p => p.PedidoId == id);
        }

        public async Task<List<Pedido>> ObtenerTodosConRelacionesAsync()
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .ToListAsync();
        }
    }
}
