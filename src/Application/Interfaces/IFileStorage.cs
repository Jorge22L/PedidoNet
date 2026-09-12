using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IFileStorage
    {
        Task<string> GuardarAsync(
            int productoId,
            Stream stream,
            string nombreArchivo,
            string contentType,
            CancellationToken cancellationToken = default);

        Task EliminarAsync(
            string ruta,
            CancellationToken cancellationToken = default);
    }
}
