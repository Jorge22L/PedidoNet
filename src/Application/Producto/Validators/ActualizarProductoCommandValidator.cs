using FluentValidation;

namespace Application.Producto.Commands.Validators
{
    public class ActualizarProductoCommandValidator : AbstractValidator<ActualizarProductoCommand>
    {
        public ActualizarProductoCommandValidator()
        {

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("Nombre es requerido")
                .MaximumLength(100).WithMessage("Máximo 100 caracteres");

            RuleFor(x => x.PrecioVenta)
                .GreaterThan(0).WithMessage("Precio debe ser mayor a 0");

            RuleFor(x => x.Existencias)
                .GreaterThanOrEqualTo(0).WithMessage("Existencias no pueden ser negativas");
        }
    }
}