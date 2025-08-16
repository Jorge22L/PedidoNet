using Application.Clientes.Commands;
using Application.Common.Mappings;
using Application.Interfaces;
using Application.Producto.Commands;
using Application.Producto.Commands.Validators;
using FluentValidation;
using Infrastructure.Services;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuración Global de Mapster
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(MappingConfig).Assembly);

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// Conexion a la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Evitar minimal APIs
builder.Services.AddControllers();

builder.Services.AddScoped<IValidator<CrearProductoCommand>, CrearProductoCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearProductoCommandValidator>();
builder.Services.AddScoped<IValidator<ActualizarProductoCommand>,  ActualizarProductoCommandValidator>();

// Agregando servicios de infraestructura
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProductoService, ProductoService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Usar controladores
app.MapControllers();

app.Run();
