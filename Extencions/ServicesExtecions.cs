using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json.Serialization;

namespace APKVersionControlAPI.Extencions
{
    public static class ServicesExtecions
    {
        public static void ConfigureCords(this IServiceCollection services, IConfiguration config)
        {
            // Leer la lista de orígenes permitidos desde la configuración
            var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins is null || allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("No allowed origins have been defined in the settings. Make sure to add the 'AllowedOrigins' section in appsettings.");
            }

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins)   // Acepta cualquier origen
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();

                });
            });
        }

        public static void ConfigureMaxRequestBodySize(this IServiceCollection services, long maxSizeInBytes)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = maxSizeInBytes;
            });
        }

        public static void ConfigureNewtonsoftJsonForControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                });
        }
    }
}
