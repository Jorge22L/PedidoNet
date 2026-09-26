using Application.Interfaces;
using Application.Producto.Commands;
using Application.Producto.Queries;
using Application.ProductoImagen.Queries;
using Domain.Abstractions;
using Domain.Entities;
using Domain.Repositories;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IUnitofWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorage _fileStorage;

        public ProductoService(IProductoRepository productoRepository, IUnitofWork unitOfWork, IMapper mapper, IFileStorage fileStorage)
        {
            _productoRepository = productoRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorage = fileStorage;
        }

        public async Task<bool> ActualizarProductoAsync(int id, ActualizarProductoCommand command)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return false;

            _mapper.Map(command, producto);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ProductoImagenDto?> AgregarImagenAsync(int productoId, Stream stream, string nombreArchivo, string contentType, long longitud, bool esPrincipal, CancellationToken cancellationToken = default)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(productoId);

            if (producto is null)
            {
                return null;
            }

            var ruta = await _fileStorage.GuardarAsync(productoId, stream, nombreArchivo, contentType, cancellationToken);

            try
            {
                if (esPrincipal)
                {
                    foreach (var imagen in producto.Imagenes)
                    {
                        imagen.EsPrincipal = false;
                    }
                }

                /* si es la primera imagen, automáticamente puede 
                     convertirse en la principal */

                if (producto.Imagenes.Count == 0)
                {
                    esPrincipal = true;
                }

                var productoImagen = new ProductoImagen
                {
                    Ruta = ruta,
                    NombreOriginal = nombreArchivo,
                    ContentType = contentType,
                    EsPrincipal = esPrincipal
                };

                producto.Imagenes.Add(productoImagen);

                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<ProductoImagenDto>(productoImagen);
            }
            catch
            {
                /*Evitar archivos huerfanos si falla SQL Server*/
                await _fileStorage.EliminarAsync(ruta, cancellationToken);
                throw;
            }

        }

        public async Task<ProductoDto> CrearProductoAsync(CrearProductoCommand command, CancellationToken cancellationToken = default)
        {
            var existente = await _productoRepository.ObtenerPorClientIdAsync(command.ClientId, cancellationToken);

            if(existente is not null)
            {
                return _mapper.Map<ProductoDto>(existente);
            }

            var producto = _mapper.Map<Producto>(command);

            await _productoRepository.AgregarAsync(producto, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ProductoDto>(producto);
        }

        public Task<bool> EliminarImagenAsync(int productoId, int imagenId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return false;

            _productoRepository.Eliminar(producto);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return null;

            return _mapper.Map<ProductoDto>(producto);

        }

        public async Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _productoRepository.ObtenerTodosAsync();

            return _mapper.Map<List<ProductoDto>>(productos);
        }
    }
}
