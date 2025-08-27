// Se crea el "builder" de la aplicación: lee args, configura loggin, config, etc.
var builder = WebApplication.CreateBuilder(args);

// Contruye la aplicación ASP.NET Core con lo registrado arriba.
var app = builder.Build();

// Endpoint raíz simple para ver que el host responde.
app.MapGet("/", () => Results.Text("SimpleCRM Gateway: Ok", "text/plain"));

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
