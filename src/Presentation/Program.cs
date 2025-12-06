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
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Predeterminado = EntraId Bearer
    .AddJwtBearer(options =>
    {
        // URL del "Authority" = endpoint de Entra ID para validar tokens
        options.Authority = builder.Configuration["Authentication:EntraId:Authority"];

        // Parámetros de validación del token (Entra ID)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = builder.Configuration.GetSection("Authentication:EntraId:ValidIssuers").Get<string[]>(),
            ValidateAudience = true,
            ValidAudiences = builder.Configuration.GetSection("Authentication:EntraId:ValidAudiences").Get<string[]>(),
            ValidateLifetime = true,
        };
    })
    // Esquema adicional para tokens internos emitidos por AuthService (desarrollo)
    .AddJwtBearer("Internal", options =>
    {
        var keyBase64 = builder.Configuration["Jwt:SigningKeyBase64"];
        if (!string.IsNullOrEmpty(keyBase64))
        {
            var key = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true
            };
        }
    });

// Autorizar con cualquiera de los dos esquemas (EntraId o Internal)
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(new[] { JwtBearerDefaults.AuthenticationScheme, "Internal" })
        .RequireAuthenticatedUser()
        .Build();
});

// Registrar HttpClient para llamar al AuthService desde el gateway
builder.Services.AddHttpClient("authclient", client =>
{
    // Intentamos leer la dirección del authservice desde la configuración de ReverseProxy
    var authAddr = builder.Configuration["ReverseProxy:Clusters:AuthService:Destinations:AuthRoute1:Address"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(authAddr);
});

// Construye la aplicación ASP.NET Core con lo registrado arriba.
var app = builder.Build();

// Se activa la política CORS en el pipeline
app.UseCors("Default");

// Middleware: intercambia token externo por interno llamando a AuthService
// Ejecutar ANTES de la autenticación para permitir que el gateway reemplace
// el header `Authorization` por el token interno antes de que ASP.NET valide el token.
app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        // Evitar intercambio únicamente para la ruta de intercambio del propio AuthService
        // y el healthcheck; permitir intercambio para otras rutas bajo /api/auth (p.ej. /api/auth/users)
        if (path.StartsWith("/api/auth/exchange", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/healthz", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var authHeader = ctx.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            await next();
            return;
        }

        // Llamar al AuthService para obtener token interno
        try
        {
            var httpFactory = app.Services.GetRequiredService<IHttpClientFactory>();
            var client = httpFactory.CreateClient("authclient");
            // Llamada directa al AuthService: la ruta en el servicio es "/exchange" (sin prefijo "/api/auth").
            var req = new HttpRequestMessage(HttpMethod.Post, "/exchange");
            req.Headers.Add("Authorization", authHeader);

            var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                ctx.Response.StatusCode = (int)resp.StatusCode;
                await ctx.Response.WriteAsync("Token exchange failed");
                return;
            }

            // Preferir el header Authorization en la respuesta desde AuthService
            if (resp.Headers.TryGetValues("Authorization", out var authValues))
            {
                var headerVal = authValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(headerVal))
                {
                    // Replace the incoming Authorization header with the internal token
                    ctx.Request.Headers["Authorization"] = headerVal;
                }
            }
        }
        catch
        {
            ctx.Response.StatusCode = StatusCodes.Status502BadGateway;
            await ctx.Response.WriteAsync("Error contacting auth service");
            return;
        }

        await next();
    });

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
