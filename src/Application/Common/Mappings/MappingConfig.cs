using Application.Clientes.Commands;
using Application.Clientes.Queries;
using Application.DetallePedido.Commands;
using Application.DetallePedido.Queries;
using Application.Pedidos.Commands;
using Application.Pedidos.Queries;
using Application.Producto.Commands;
using Application.Producto.Queries;
using Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Mappings
{
    public class MappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Entidad <-> DTO
            config.NewConfig<Domain.Entities.Producto, ProductoDto>();
            config.NewConfig<ProductoDto, Domain.Entities.Producto>();
            config.NewConfig<Cliente, ClienteDto>();
            config.NewConfig<ClienteDto, Cliente>();
            config.NewConfig<Pedido, PedidoDto>()
                .Map(dest => dest.ClienteNombre, src => src.Cliente.Nombre);
            config.NewConfig<PedidoDto, Pedido>();
            config.NewConfig<Domain.Entities.DetallePedido, DetallePedidoDto>()
                .Map(dest => dest.ProductoNombre, src => src.Producto.Nombre)
                .Map(dest => dest.ProductoCodigo, src => src.Producto.Codigo)
                .Map(dest => dest.TieneIVA, src => src.Producto.TieneIVA ?? false);

            // Command <-> Entidad
            //Producto
            config.NewConfig<CrearProductoCommand, Domain.Entities.Producto>();
            config.NewConfig<ActualizarProductoCommand, Domain.Entities.Producto>();
            // Cliente
            config.NewConfig<CrearClienteCommand, Cliente>();
            config.NewConfig<ActualizarClienteCommand, Cliente>();
            // Pedidos
            config.NewConfig<CrearPedidoCommand, Pedido>()
                .Map(dest => dest.Estado, src => "Pendiente")
                .Ignore(dest => dest.PedidoId);

            config.NewConfig<ActualizarPedidoCommand, Pedido>()
                .Ignore(dest => dest.PedidoId)
                .Ignore(dest => dest.Detalles);
            //Detalle Pedidos
            config.NewConfig<DetallePedidoCommand, Domain.Entities.DetallePedido>()
                .Ignore(dest => dest.DetalleId)
                .Ignore(dest => dest.PedidoId);

            config.NewConfig<List<DetallePedidoCommand>, List<Domain.Entities.DetallePedido>>()
                .MapWith(src => src.Adapt<List<Domain.Entities.DetallePedido>>());

            config.NewConfig<List<Domain.Entities.DetallePedido>, List<DetallePedidoDto>>()
                .MapWith(src => src.Adapt<List<DetallePedidoDto>>());



        }
    }
}
