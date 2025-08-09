using Application.Interfaces;
using Application.Producto.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductoController(IProductoService productoService)
        {
            _productoService = productoService;
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CrearProductoCommand command)
        {
            var id = await _productoService.CrearProductoAsync(command);
            return CreatedAtAction(nameof(Get), new { id }, command);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] ActualizarProductoCommand command)
        {
            var actualizado = await _productoService.ActualizarProductoAsync(id, command);
            if(!actualizado) return NotFound();
            return Ok(actualizado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado = await _productoService.EliminarProductoAsync(id);
            if(!eliminado) return NotFound();
            return Ok(eliminado);
        }
    }
}
