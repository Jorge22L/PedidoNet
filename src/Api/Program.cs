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
using Middleware;
using Persistence;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Domain.Repositories;
using Infrastructure.Repositories;
using Application.Interfaces.Repositories;
using Domain.Abstractions;

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

// Evitar minimal APIs
builder.Services.AddControllers();

builder.Services.AddScoped<IValidator<CrearProductoCommand>, CrearProductoCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearProductoCommandValidator>();
builder.Services.AddScoped<IValidator<ActualizarProductoCommand>,  ActualizarProductoCommandValidator>();

// Agregando servicios de infraestructura
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitofWork, EfUnitOfWork>();

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Agregando JWT

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no está configurado");

var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no está configurado");

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no está configurado");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

// Crear Policy
builder.Services.AddAuthorizationBuilder()
                   // Crear Policy
                   .AddPolicy("Pedidos.Read", policy =>
        {
            policy.RequireAuthenticatedUser();
        })
                   // Crear Policy
                   .AddPolicy("Pedidos.Create", policy =>
        {
            policy.RequireRole("Administrador", "Vendedor");
        });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Pedidos",
        Version = "v1",
        Description = "Documentación de la API de Pedidos"
    });

    const string securitySchemeName = "Bearer";

    c.AddSecurityDefinition(
        securitySchemeName,
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Ingrese: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    c.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    securitySchemeName,
                    document)
            ] = []
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existeAdmin = await context.Usuarios.AnyAsync(x => x.NombreUsuario == "admin");

        if (!existeAdmin)
        {
            var usuario = new Usuario
            {
                NombreUsuario = "admin",
                Rol = "Administrador",
                Activo = true
            };

            var passwordHasher = new PasswordHasher<Usuario>();

            usuario.PasswordHash = passwordHasher.HashPassword(usuario, "Admin123*");
            context.Usuarios.Add(usuario);

            await context.SaveChangesAsync();
        }
    }

    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "API v1");
    });

    app.MapGet(
            "/swagger/{**any}",
            () => Results.Redirect(
                "/swagger/index.html"))
        .AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseGlobalExceptionHandler();

// Usar controladores
app.MapControllers();

app.Run();
