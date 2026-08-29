using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int Existencias { get; set; }
        public bool? TieneIVA { get; set; }
        public bool? TieneISC { get; set; }

        public void DescontarExistencia(int cantidad)
        {
            if (cantidad <= 0)
            {
                throw new DomainException(
                    "La cantidad a descontar debe ser mayor que cero.");
            }

            if (Existencias < cantidad)
            {
                throw new DomainException(
                    $"Stock insuficiente para el producto '{Nombre}'. " +
                    $"Disponible: {Existencias}, solicitado: {cantidad}.");
            }

            Existencias -= cantidad;
        }


        public void ReponerExistencia(int cantidad)
        {
            if (cantidad <= 0)
            {
                throw new DomainException(
                    "La cantidad a reponer debe ser mayor que cero.");
            }

            Existencias += cantidad;
        }
    }
}
