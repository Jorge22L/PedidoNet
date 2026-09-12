using Application.DetallePedido.Commands;
using Application.Exceptions;
using Application.Interfaces;
using Application.Pedidos.Commands;
using Domain.Exceptions;
using Domain.Repositories;
using Infrastructure.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Repositories
{
    public class PedidoWriteRepository : IPedidoWriteRepository
    {
        private readonly ApplicationDbContext _context;

        public PedidoWriteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ActualizarAsync(int pedidoId, ActualizarPedidoCommand command, CancellationToken cancellationToken = default)
        {
            var connection = _context.Database.GetDbConnection();
            var debeCerrarConexion = connection.State != System.Data.ConnectionState.Open;
            if (debeCerrarConexion)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var sqlCommand = connection.CreateCommand();

                sqlCommand.CommandText = "dbo.ActualizarPedido";
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                AgregarParametro(sqlCommand, "@PedidoId", pedidoId);
                AgregarParametro(sqlCommand, "@ClienteId", command.ClienteId);
                AgregarParametro(sqlCommand, "@FormaPago", string.IsNullOrWhiteSpace(command.FormaPago) ? null : command.FormaPago);

                var detallesJson = command.Detalles is { Count: > 0 } ? SerializarDetalles(command.Detalles) : null;

                AgregarParametro(sqlCommand, "@DetallesJson", detallesJson);

                await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);

                return await reader.ReadAsync(cancellationToken);
            }
            catch(SqlException e)
            {
                throw TraducirSqlException(e);
            }
            finally
            {
                if (debeCerrarConexion)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<bool> CancelarAsync(int pedidoId, CancellationToken cancellationToken = default)
        {
            return await EjecutarOperacionEstadoAsync("dbo.CancelarPedido", pedidoId, "Cancelado", cancellationToken);
        }

        public async Task<bool> CompletarAsync(int pedidoId, CancellationToken cancellationToken = default)
        {
            return await EjecutarOperacionEstadoAsync("dbo.CompletarPedido", pedidoId, "Completado", cancellationToken);
        }

        public async Task<bool> EliminarAsync(int pedidoId, CancellationToken cancellationToken = default)
        {
            return await EjecutarOperacionEstadoAsync("dbo.EliminarPedido", pedidoId, "Eliminado", cancellationToken);
        }

        public async Task<int> GuardarAsync(CrearPedidoCommand command, CancellationToken cancellationToken = default)
        {
            var detallesJson = SerializarDetalles(command.Detalles);
            var connection = _context.Database.GetDbConnection();

            var debeCerrarConexion = connection.State != System.Data.ConnectionState.Open;

            if (debeCerrarConexion)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var sqlCommand = connection.CreateCommand();

                sqlCommand.CommandText = "dbo.GuardarPedido";
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                AgregarParametro(sqlCommand, "@ClienteId", command.ClienteId);
                AgregarParametro(sqlCommand, "@FormaPago", command.FormaPago);
                AgregarParametro(sqlCommand, "@DetallesJson", detallesJson);

                await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
                if(!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("El procedimiento GuardarPedido no devolvió el pedido creado");
                }

                var pedidoIdOrdinal = reader.GetOrdinal("PedidoId");

                return reader.GetInt32(pedidoIdOrdinal);
            }
            catch(SqlException e)
            {
                throw TraducirSqlException(e);
            }
            finally
            {
                if (debeCerrarConexion)
                {
                    await connection.CloseAsync();
                }
            }
        }
        private static Exception TraducirSqlException(SqlException exception)
        {
            return exception.Number switch
            {
                50001 => new Application.Exceptions.NotFoundException(exception.Message),
                50002 or 50003 or
                50004 or 50005 or
                50006 or 50008 => new BusinessRuleException(exception.Message),

                50007 => new NotFoundException(
            exception.Message),

                50009 => new DomainException(
                    exception.Message),

                // ActualizarPedido
                50101 => new NotFoundException(
                    exception.Message),

                50102 => new DomainException(
                    exception.Message),

                50103 => new NotFoundException(
                    exception.Message),

                50104 or 50105 or
                50106 or 50107 or
                50108 or 50110 =>
                    new BusinessRuleException(
                        exception.Message),

                50109 => new NotFoundException(
                    exception.Message),

                50111 => new DomainException(
                    exception.Message),

                // Eliminar
                50201 => new NotFoundException(
                    exception.Message),

                50202 => new DomainException(
                    exception.Message),

                // Completar
                50301 => new NotFoundException(
                    exception.Message),

                50302 => new DomainException(
                    exception.Message),

                // Cancelar
                50401 => new NotFoundException(
                    exception.Message),

                50402 => new DomainException(
                    exception.Message),

                _ => exception
            };

        }

        private async Task<bool> EjecutarOperacionEstadoAsync(string storedProcedure, int pedidoId, string columnaResultado, CancellationToken cancellationToken)
        {
            var connection = _context.Database.GetDbConnection();
            var debeCerrarConexion = connection.State != System.Data.ConnectionState.Open;

            if (debeCerrarConexion)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var sqlCommand = connection.CreateCommand();

                sqlCommand.CommandText = storedProcedure;

                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                AgregarParametro(sqlCommand, "@PedidoId", pedidoId);

                await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);

                if(!await reader.ReadAsync(cancellationToken))
                {
                    return false;
                }

                var ordinal = reader.GetOrdinal(columnaResultado);

                return reader.GetBoolean(ordinal);
            }
            catch(SqlException e)
            {
                throw TraducirSqlException(e);
            }
            finally
            {
                if (debeCerrarConexion)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static string SerializarDetalles(IEnumerable<DetallePedidoCommand> detalles)
        {
            var detallesSp = detalles.Select(detalle =>

                new PedidoDetallesSpModel
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Descuento = detalle.Descuento
                }).ToList();

            return JsonSerializer.Serialize(detallesSp);
        }

        private static void AgregarParametro(DbCommand command, string nombre, object? valor)
        {
            var parametro = command.CreateParameter();
            parametro.ParameterName = nombre;
            parametro.Value = valor ?? DBNull.Value;
            command.Parameters.Add(parametro);
        }

        
    }
}
