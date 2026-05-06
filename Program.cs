using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;
using ProyectoInnovador.Security.Services;

var builder = WebApplication.CreateBuilder(args);

//configuracion de Servicios
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=cerberus.db"));

builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection("S3"));

builder.Services.AddSingleton<ICheck1KeyProvider, ConfigurationCheck1KeyProvider>();
builder.Services.AddScoped<IAesRoundtripService, AesCbcRoundtripService>();
builder.Services.AddScoped<IIntegrityHashService, Sha256IntegrityHashService>();
builder.Services.AddScoped<IFileCryptoService, FileCryptoService>();
builder.Services.AddScoped<ISecurityCheckOrchestrator, SecurityCheckOrchestrator>();
builder.Services.AddScoped<IFileOrchestrator, FileOrchestrator>();

//registro de S3 para MinIO (Tarea 1)
builder.Services.AddSingleton<S3Service>();
builder.Services.AddSingleton<AzureService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
