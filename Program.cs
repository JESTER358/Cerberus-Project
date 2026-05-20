using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;
using ProyectoInnovador.Security.Services;

var builder = WebApplication.CreateBuilder(args);

// Aumentar limites para archivos pesados
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

//configuracion de Servicios
builder.Services.AddControllersWithViews();

// Sesión — requerido para autenticación manual (sin ASP.NET Identity)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// SQLite con path persistente en Azure App Service.
// En Azure, D:\home\data sobrevive restarts y deploys (almacenamiento persistente montado).
// En local, usa el directorio de la app normalmente.
var dbFolder = Environment.GetEnvironmentVariable("CERBERUS_DB_PATH")
    ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbFolder, "cerberus.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection("S3"));

builder.Services.AddSingleton<ICheck1KeyProvider, ConfigurationCheck1KeyProvider>();
builder.Services.AddScoped<IAesRoundtripService, AesCbcRoundtripService>();
builder.Services.AddScoped<IIntegrityHashService, Sha256IntegrityHashService>();
builder.Services.AddScoped<IFileCryptoService, FileCryptoService>();
builder.Services.AddScoped<ISecurityCheckOrchestrator, SecurityCheckOrchestrator>();
builder.Services.AddScoped<IFileOrchestrator, FileOrchestrator>();

//registro de S3 para MinIO (Tarea 1)
builder.Services.AddSingleton<S3Service>();

// Azure Blob Storage — inyección limpia vía interfaz (IAzureService → AzureService)
builder.Services.AddSingleton<IAzureService, AzureService>();

var app = builder.Build();

// Auto-migrate: aplica todas las migraciones pendientes al arrancar.
// Si cerberus.db no existe, lo crea. Si ya existe, solo aplica lo que falta.
// Crítico para Azure App Service donde el archivo empieza desde cero en cada deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
