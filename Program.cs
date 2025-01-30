using APKVersionControlAPI.Extencions;
using APKVersionControlAPI.Infrastructure;
using APKVersionControlAPI.Infrastructure.Repository;
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using HealthCenterAPI.Extencion;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


// Agregar SqlLiteContext al contenedor de servicios
builder.Services.AddDbContext<SqlLiteContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "apkVersing.db")}"));

builder.Services.ConfigureMaxRequestBodySize(2L * 1024 * 1024 * 1024); // 2 GB
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<IISOptions>(options => { });
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddScoped<IAPKVersionControlServices, APKVersionControlServices>();
builder.Services.AddScoped<IApkFileRepository, ApkFileRepository>();
builder.Services.ConfigureBackgroundJobs();
builder.Services.AddHangfireServer();
builder.Services.AddOpenApi();
builder.Services.ConfigureCords(builder.Configuration);
builder.Services.ConfigureNewtonsoftJsonForControllers();

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

// Asegura que la base de datos esté creada
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SqlLiteContext>();
    dbContext.Database.EnsureCreated(); // Crea la base de datos y las tablas si no existen
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

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

SQLitePCL.Batteries.Init();
app.UseCors("CorsPolicy");
app.UseRouting();
app.MapControllers();
app.Run();
