using Application.Common.Files;
using Application.Interfaces;
using Application.Producto.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly IValidator<CrearProductoCommand> _crearProductoCommandValidator;
        private readonly IValidator<ActualizarProductoCommand> _actualizarProductoCommandValidator;

        public ProductoController(IProductoService productoService, IValidator<CrearProductoCommand> crearProductoValidator, IValidator<ActualizarProductoCommand> actualizarProductoValidator)
        {
            _productoService = productoService;
            _crearProductoCommandValidator = crearProductoValidator;
            _actualizarProductoCommandValidator = actualizarProductoValidator;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var productos = await _productoService.ObtenerTodosAsync();
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);
            if (producto == null) return NotFound();

            return Ok(producto);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CrearProductoCommand command)
        {

            var validation = _crearProductoCommandValidator.Validate(command);

            if (!validation.IsValid)
            {
                return BadRequest(FormatValidationErrors(validation));
            }
            else
            {
                var id = await _productoService.CrearProductoAsync(command);
                return CreatedAtAction(nameof(Get), new { id }, command);
            }

                
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] ActualizarProductoCommand command)
        {
            var actualizado = await _productoService.ActualizarProductoAsync(id, command);
            if (!actualizado) return NotFound();
            return Ok(actualizado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado = await _productoService.EliminarProductoAsync(id);
            if (!eliminado) return NotFound();
            return Ok(eliminado);
        }

        [HttpPost("{id:int}/imagenes")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> SubirImagen(int id, IFormFile imagen, [FromForm] bool esPrincipal = false,
            CancellationToken cancellationToken = default)
        {
            if(imagen is null || imagen.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Debe proporcionar una imgen."
                });
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if(imagen.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    message = "La imagen no puede superar los 5 MB."
                });
            }

            await using var stream = imagen.OpenReadStream();

            var validation = await ImageFileValidator.ValidateAsync(stream, imagen.FileName, imagen.ContentType, cancellationToken);

            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    message = validation.Error
                });
            }

            var resultado = await _productoService.AgregarImagenAsync(id, stream, imagen.FileName,
                imagen.ContentType, imagen.Length, esPrincipal, cancellationToken);

            if(resultado is null)
            {
                return NotFound();
            }

            return Ok(resultado);
        }


        private object FormatValidationErrors(FluentValidation.Results.ValidationResult validationResult)
        {

            var detailedErrors = validationResult.Errors
                .Select(e => new
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage,
                    Code = e.ErrorCode,
                    Severity = e.Severity.ToString()
                })
                .ToList();

            // For teaching purposes, return both formats to show the difference
            return new
            {
                Title = "Validation Failed",
                Status = 400,
                //SimpleErrors = simpleErrors,
                DetailedErrors = detailedErrors
            };
        }
    }
}
