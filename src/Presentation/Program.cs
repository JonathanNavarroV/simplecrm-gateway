// Se crea el "builder" de la aplicación: lee args, configura loggin, config, etc.
var builder = WebApplication.CreateBuilder(args);

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

// Contruye la aplicación ASP.NET Core con lo registrado arriba.
var app = builder.Build();

// Se actuva la política CORS en el pipeline
app.UseCors("Default");

// Endpoint raíz simple para ver que el host responde.
app.MapGet("/", () => Results.Text("SimpleCRM Gateway (con CORS): Ok", "text/plain"));

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

// Arranca el servidor web (Kestrel) y queda escuchando peticiones HTTP.
app.Run();
