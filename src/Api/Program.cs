using Application.Clientes.Commands;
using Application.Common.Mappings;
using Application.Interfaces;
using Application.Producto.Commands;
using Application.Producto.Commands.Validators;
using FluentValidation;
using Infrastructure.Services;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Middleware;
using Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Domain.Repositories;
using Infrastructure.Repositories;
using Domain.Abstractions;
using Api.Services;
using Asp.Versioning;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Api.Configuration;
using Asp.Versioning.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

// Configuración Global de Mapster
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(MappingConfig).Assembly);

builder.Configuration.AddKeyPerFile(directoryPath: "/run/secrets", optional: true);


builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// Conexion a la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("No se encontró la cadena de conexión"))); 

builder.Services.AddEndpointsApiExplorer();

// Evitar minimal APIs
builder.Services.AddControllers();

/*
 * Obliga al cliente/consumidor a especificar la versión de la API
 * en la URL, en vez de aceptar una API sin versión
 */
builder.Services
    .AddApiVersioning(opt =>
    {
        opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        opt.AssumeDefaultVersionWhenUnspecified = false;
        opt.ReportApiVersions = true;
        opt.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(opt =>
    {
        opt.GroupNameFormat = "'v'VVV";
        opt.SubstituteApiVersionInUrl = true;
    });

// Agregando CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
builder.Services.AddScoped<IPedidoWriteRepository, PedidoWriteRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

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

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(c =>
{
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

    var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwaggerUI(c =>
    {
        foreach(var description in apiVersionProvider.ApiVersionDescriptions.Reverse())
        {
            c.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"PedidoNet API {description.GroupName.ToUpperInvariant()}"
                );
        }
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();


// Usar controladores
app.MapControllers();

app.Run();
