using Hackathon_Docente_2026.Data;
using Microsoft.EntityFrameworkCore;
using Hackathon_Docente_2026.Components;

if (args.Length > 0 && args[0] == "--qti-test")
{
    var ruta = args.Length > 1 ? args[1] : "qti-prueba.zip";
    File.WriteAllBytes(ruta, new Hackathon_Docente_2026.Services.QtiExportService().GenerarZipDePrueba());
    Console.WriteLine($"ZIP generado: {Path.GetFullPath(ruta)}");
    return;
}

// Carga .env si existe (las variables ya definidas en el sistema tienen prioridad).
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// AUTH_USUARIO / AUTH_PASSWORD sobrescriben appsettings; si no existen se conservan los valores por defecto.
var authOverrides = new Dictionary<string, string?>();
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AUTH_USUARIO")))
    authOverrides["Auth:Usuario"] = Environment.GetEnvironmentVariable("AUTH_USUARIO");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AUTH_PASSWORD")))
    authOverrides["Auth:Password"] = Environment.GetEnvironmentVariable("AUTH_PASSWORD");
builder.Configuration.AddInMemoryCollection(authOverrides);

builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=hackathon.db"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<Hackathon_Docente_2026.Services.AuthState>();
builder.Services.AddSingleton<Hackathon_Docente_2026.Services.PdfExtractionService>();
builder.Services.AddSingleton<Hackathon_Docente_2026.Services.QtiExportService>();
builder.Services.AddHttpClient<Hackathon_Docente_2026.Services.QuestionGenerationService>(c => c.Timeout = TimeSpan.FromMinutes(3));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>()
        .CreateDbContext().Database.Migrate();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
