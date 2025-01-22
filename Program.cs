using APKVersionControlAPI.Infrastructure.Repository;
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using HealthCenterAPI.Extencion;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;
using Scalar.AspNetCore;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<IISOptions>(options => { });
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddScoped<IAPKVersionControlServices, APKVersionControlServices>();
builder.Services.AddScoped<IApkProcessor, ApkProcessor>();
builder.Services.ConfigureBackgroundJobs();
builder.Services.AddHangfireServer();
builder.Services.AddOpenApi();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()   // Acepta cualquier origen
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});



builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
{
    app.UseHsts();    // Habilitar HSTS (HTTP Strict Transport Security)

    // A�adir encabezados de seguridad
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; script-src 'none';"; // Restrict APIs
        await next();
    });

    app.UseHttpsRedirection();
}


// Configuración de archivos estáticos basada en el sistema operativo
if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    string webRootPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!, "wwwroot");

    if (!Directory.Exists(webRootPath))
    {
        Directory.CreateDirectory(webRootPath);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
        //RequestPath = "/static",
        OnPrepareResponse = ctx =>
        {
            // Configuración de caché para archivos estáticos
            const int durationInSeconds = 60 * 60 * 24 * 7; // 7 días
            ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={durationInSeconds}");

            // Bloquear acceso a la carpeta Files
            if (ctx.File.PhysicalPath!.Contains(Path.Combine("wwwroot", "Files")))
            {
                ctx.Context.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Context.Response.ContentLength = 0;
                ctx.Context.Response.Body = Stream.Null;
            }
        }
    });

}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    app.UseStaticFiles();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Configurar Scalar para la interfaz de usuario
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("APKVersionControl")
               .WithDownloadButton(false)
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios);
    });

    app.UseHangfireDashboard();
}

app.UseRouting();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
