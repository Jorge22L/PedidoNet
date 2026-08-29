using Application.Interfaces;
using Application.Pedidos.Commands;
using Application.Pedidos.Queries;
using Application.DetallePedido.Queries;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Application.Exceptions;
using MapsterMapper;
using Mapster;
using Domain.Constantes;
using Domain.Repositories;
using Application.Interfaces.Repositories;
using Domain.Abstractions;

namespace Infrastructure.Services
{
    public class PedidoService : IPedidoService
    {
        private const decimal TasaIva = 0.15m;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitofWork _unitOfWork;
        private readonly IMapper _mapper;

        public PedidoService(IPedidoRepository pedidoRepository, IProductoRepository productoRepository,
            IClienteRepository clienteRepository, IUnitofWork unitOfWork, IMapper mapper)
        {
            _pedidoRepository = pedidoRepository;
            _productoRepository = productoRepository;
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> CrearPedidoAsync(CrearPedidoCommand command)
        {
            await using var transaction =
        await _unitOfWork.BeginTransactionAsync();

            try
            {
                /*
                 * Application verifica existencia porque eso
                 * requiere acceder a persistencia.
                 */
                var clienteExiste =
                    await _clienteRepository
                        .ExisteAsync(command.ClienteId);

                if (!clienteExiste)
                {
                    throw new ArgumentException(
                        "El cliente especificado no existe.");
                }

                var productosIds =
                    command.Detalles
                        .Select(x => x.ProductoId)
                        .Distinct()
                        .ToList();

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(productosIds);

                if (productos.Count != productosIds.Count)
                {
                    throw new ArgumentException(
                        "Uno o más productos no existen.");
                }

                var productosPorId =
                    productos.ToDictionary(
                        p => p.ProductoId);

                var pedido = new Pedido
                {
                    ClienteId = command.ClienteId,

                    Fecha = command.Fecha,

                    Descuento = command.Descuento,

                    FormaPago = command.FormaPago
                };

                /*
                 * Las reglas importantes ahora viven
                 * dentro del Domain.
                 */
                foreach (var detalleCommand
                         in command.Detalles)
                {
                    var producto =
                        productosPorId[
                            detalleCommand.ProductoId];

                    pedido.AgregarDetalle(
                        producto,
                        detalleCommand.Cantidad,
                        detalleCommand.Descuento);
                }

                pedido.RecalcularTotales();

                await _pedidoRepository
                    .AgregarAsync(pedido);

                await _unitOfWork
                    .SaveChangesAsync();

                await transaction.CommitAsync();

                return pedido.PedidoId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ActualizarPedidoAsync(
            int id,
            ActualizarPedidoCommand command)
        {
            await using var transaction =
        await _unitOfWork
            .BeginTransactionAsync();

            try
            {
                var pedido =
                    await _pedidoRepository
                        .ObtenerPorIdAsync(
                            id,
                            incluirDetalles: true);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                /*
                 * Domain decide si se puede modificar.
                 */
                pedido.ValidarPuedeModificarse();

                if (command.ClienteId > 0)
                {
                    var clienteExiste =
                        await _clienteRepository
                            .ExisteAsync(
                                command.ClienteId);

                    if (!clienteExiste)
                    {
                        throw new ArgumentException(
                            "El cliente especificado no existe.");
                    }

                    pedido.ClienteId =
                        command.ClienteId;
                }

                if (command.Fecha.HasValue)
                {
                    pedido.Fecha =
                        command.Fecha.Value;
                }

                if (command.Descuento.HasValue)
                {
                    pedido.Descuento =
                        command.Descuento.Value;
                }

                if (!string.IsNullOrWhiteSpace(
                        command.FormaPago))
                {
                    pedido.FormaPago =
                        command.FormaPago;
                }

                if (command.Detalles is
                    { Count: > 0 })
                {
                    /*
                     * 1. Recuperar productos anteriores.
                     */
                    var productosAnterioresIds =
                        pedido.Detalles
                            .Select(d => d.ProductoId)
                            .Distinct()
                            .ToList();

                    var productosAnteriores =
                        await _productoRepository
                            .ObtenerPorIdsAsync(
                                productosAnterioresIds);

                    var anterioresPorId =
                        productosAnteriores
                            .ToDictionary(
                                p => p.ProductoId);

                    /*
                     * 2. Reponer inventario anterior.
                     */
                    foreach (var detalle
                             in pedido.Detalles)
                    {
                        if (!anterioresPorId.TryGetValue(
                                detalle.ProductoId,
                                out var producto))
                        {
                            throw new InvalidOperationException(
                                $"No se encontró el producto " +
                                $"{detalle.ProductoId}.");
                        }

                        producto.ReponerExistencia(
                            detalle.Cantidad);
                    }

                    /*
                     * 3. Eliminar antiguos detalles.
                     */
                    var detallesAnteriores =
                        pedido.Detalles.ToList();

                    _pedidoRepository
                        .EliminarDetalles(
                            detallesAnteriores);

                    pedido.Detalles.Clear();

                    /*
                     * 4. Obtener nuevos productos.
                     */
                    var nuevosProductosIds =
                        command.Detalles
                            .Select(d => d.ProductoId)
                            .Distinct()
                            .ToList();

                    var nuevosProductos =
                        await _productoRepository
                            .ObtenerPorIdsAsync(
                                nuevosProductosIds);

                    if (nuevosProductos.Count !=
                        nuevosProductosIds.Count)
                    {
                        throw new ArgumentException(
                            "Uno o más productos no existen.");
                    }

                    var nuevosProductosPorId =
                        nuevosProductos
                            .ToDictionary(
                                p => p.ProductoId);

                    /*
                     * 5. El Domain vuelve a crear los detalles
                     *    y valida stock, duplicados, precios,
                     *    descuentos e IVA.
                     */
                    foreach (var detalleCommand
                             in command.Detalles)
                    {
                        var producto =
                            nuevosProductosPorId[
                                detalleCommand.ProductoId];

                        pedido.AgregarDetalle(
                            producto,
                            detalleCommand.Cantidad,
                            detalleCommand.Descuento);
                    }
                }

                /*
                 * Domain calcula los importes.
                 */
                pedido.RecalcularTotales();

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool>CompletarPedidoAsync(int id)
        {
            var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);

            if(pedido == null)
            {
                return false;
            }

            pedido.Completar();

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool>CancelarPedidoAsync(int id)
        {
            await using var transaction =
        await _unitOfWork
            .BeginTransactionAsync();

            try
            {
                var pedido =
                    await _pedidoRepository
                        .ObtenerPorIdAsync(
                            id,
                            incluirDetalles: true);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                /*
                 * El Domain valida la transición.
                 */
                pedido.Cancelar();

                var productosIds =
                    pedido.Detalles
                        .Select(x => x.ProductoId)
                        .Distinct()
                        .ToList();

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(
                            productosIds);

                var productosPorId =
                    productos.ToDictionary(
                        x => x.ProductoId);

                foreach (var detalle
                         in pedido.Detalles)
                {
                    if (!productosPorId.TryGetValue(
                            detalle.ProductoId,
                            out var producto))
                    {
                        throw new InvalidOperationException(
                            $"No se encontró el producto " +
                            $"{detalle.ProductoId}.");
                    }

                    producto.ReponerExistencia(
                        detalle.Cantidad);
                }

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool>EliminarPedidoAsync(int id)
        {
            await using var transaction =
        await _unitOfWork
            .BeginTransactionAsync();

            try
            {
                var pedido =
                    await _pedidoRepository
                        .ObtenerPorIdAsync(
                            id,
                            incluirDetalles: true);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                /*
                 * Regla del dominio.
                 */
                pedido.ValidarPuedeEliminarse();

                var productosIds =
                    pedido.Detalles
                        .Select(d => d.ProductoId)
                        .Distinct()
                        .ToList();

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(
                            productosIds);

                var productosPorId =
                    productos.ToDictionary(
                        p => p.ProductoId);

                foreach (var detalle
                         in pedido.Detalles)
                {
                    if (!productosPorId.TryGetValue(
                            detalle.ProductoId,
                            out var producto))
                    {
                        throw new InvalidOperationException(
                            $"No se encontró el producto " +
                            $"{detalle.ProductoId}.");
                    }

                    producto.ReponerExistencia(
                        detalle.Cantidad);
                }

                _pedidoRepository.Eliminar(pedido);

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PedidoDto?>
            ObtenerPorIdAsync(int id)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(
                        id,
                        incluirRelaciones: true);

            if (pedido == null)
                return null;

            return _mapper.Map<PedidoDto>(
                pedido);
        }

        public async Task<List<PedidoDto>>
            ObtenerTodosAsync()
        {
            var pedidos =
                await _pedidoRepository
                    .ObtenerTodosConRelacionesAsync();

            return _mapper.Map<List<PedidoDto>>(
                pedidos);
        }

        public async Task<List<PedidoDto>>
            ObtenerPorClienteAsync(
                int clienteId)
        {
            var pedidos =
                await _pedidoRepository
                    .ObtenerPorClienteConRelacionesAsync(
                        clienteId);

            return _mapper.Map<List<PedidoDto>>(
                pedidos);
        }

        
    }
}