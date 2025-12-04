using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Se crea el "builder" de la aplicación: lee args, configura logging, config, etc.
var builder = WebApplication.CreateBuilder(args);

// Registra el YARP y carga la sección "ReverseProxy" del appsettings
// Nota: YARP reenvía headers por defecto; para añadir o transformar headers
// usar la sección "Transforms" en la configuración o configurar ProxyRequestOptions.
builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Se registra CORS en el contenedor de servicios
builder.Services.AddCors(options =>
{
    // Se define una política "Default"
    options.AddPolicy(
        "Default",
        policy =>
        {
            policy
                .AllowAnyHeader() // Se permite cualquier header (ej: Authorization)
                .AllowAnyMethod() // Se permite cualquier método (ej: GET, POST, PUT, DELETE, etc.)
                .WithOrigins("http://localhost:4200"); // Origen permitido
        }
    );
});

// Auth: Entra ID
// Se registra esquema Bearer (JWT) para validar las peticiones
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Esquema "Bearer"
    .AddJwtBearer(options =>
    {
        // URL del "Authority" = endpoint de Entra ID para validar tokens
        options.Authority = builder.Configuration["Authentication:EntraId:Authority"];

        // Parámetros de validación del token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar que el "issuer" del token sea uno de los permitidos
            ValidateIssuer = true,
            ValidIssuers = builder
                .Configuration.GetSection("Authentication:EntraId:ValidIssuers")
                .Get<string[]>(),

            // Validar que el "audience" sea válido
            ValidateAudience = true,
            ValidAudiences = builder
                .Configuration.GetSection("Authentication:EntraId:ValidAudiences")
                .Get<string[]>(),

            // Validar que el token no esté expirado
            ValidateLifetime = true,
        };
    });

// Se registra el servicio de autorización (roles, políticas, etc...)
builder.Services.AddAuthorization();

// Construye la aplicación ASP.NET Core con lo registrado arriba.
var app = builder.Build();

// Se activa la política CORS en el pipeline
app.UseCors("Default");

// Se activa la autenticación y luego la autorización
app.UseAuthentication();
app.UseAuthorization();

// Endpoint de salud SIN autenticación
app.MapGet(
    "/healthz",
    () =>
        Results.Ok(
            new
            {
                service = "simplecrm-gateway",
                status = "healthy",
                time = DateTime.UtcNow,
            }
        )
);

// Se mapea el ReverseProxy
// Protege todas las rutas proxied: requiere token Bearer válido.
// Para pruebas locales sin autenticación temporal, cambiar a `app.MapReverseProxy()`.
app.MapReverseProxy().RequireAuthorization();

// Arranca el servidor web (Kestrel) y queda escuchando peticiones HTTP.
app.Run();
