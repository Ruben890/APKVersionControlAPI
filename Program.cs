using APKVersionControlAPI.Extencions;
using APKVersionControlAPI.Infrastructure.Repository;
using APKVersionControlAPI.Interfaces.IRepository;
using APKVersionControlAPI.Interfaces.IServices;
using APKVersionControlAPI.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using HealthCenterAPI.Extencion;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.ConfigureMaxRequestBodySize(2L * 1024 * 1024 * 1024); // 2 GB
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<IISOptions>(options => { });
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddScoped<IAPKVersionControlServices, APKVersionControlServices>();
builder.Services.AddScoped<IApkProcessor, ApkProcessor>();
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

app.UseCors("CorsPolicy");
app.UseRouting();
app.MapControllers();
app.Run();
