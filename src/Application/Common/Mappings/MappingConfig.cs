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

            // Command <-> Entidad
            config.NewConfig<CrearProductoCommand, Domain.Entities.Producto>();
            config.NewConfig<ActualizarProductoCommand, Domain.Entities.Producto>();
        }
    }
}
