using Application.Producto.Commands;
using Application.Producto.Queries;
using Application.ProductoImagen.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IProductoService
    {
        Task<List<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
        Task<ProductoDto> CrearProductoAsync(CrearProductoCommand command, CancellationToken cancellationToken = default);
        Task<bool> ActualizarProductoAsync(int id, ActualizarProductoCommand command);
        Task<bool> EliminarProductoAsync(int id);
        Task<ProductoImagenDto?> AgregarImagenAsync(
            int productoId, 
            Stream stream,
            string nombreArchivo,
            string contentType,
            long longitud,
            bool esPrincipal,
            CancellationToken cancellationToken = default);

        Task<bool> EliminarImagenAsync(int productoId, int imagenId, CancellationToken cancellationToken = default);
    }
}
