
using ApiAstil.Services;

namespace ApiAstil
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Registrar servicio de datos SQL
            builder.Services.AddScoped<ISqlDataService, SqlDataService>();

            // Registrar repositorio de facturas (Dapper, independiente de ISqlDataService)
            builder.Services.AddScoped<IFacturasRepository, FacturasRepository>();

            // Registrar HttpClient para llamadas a APIs externas
            builder.Services.AddHttpClient();

            // Configurar CORS para el frontend (origenes permitidos por configuracion, sin wildcard)
            const string frontendCorsPolicy = "Frontend";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(frontendCorsPolicy, policy =>
                {
                    var allowedOrigins = builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>() ?? Array.Empty<string>();

                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Configurar Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "API Astil",
                    Description = "API creada con ASP.NET Core y Swagger",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "It Qualis",
                        Email = "desarrollo@itqualis.com.co"
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Astil V1");
                    options.RoutePrefix = string.Empty; // Swagger en la ra�z (opcional)
                });
            }

            // Solo redirigir a HTTPS en producci�n
            //if (!app.Environment.IsDevelopment())
            //{
                //app.UseHttpsRedirection();
            //}
            app.UseCors(frontendCorsPolicy);
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
