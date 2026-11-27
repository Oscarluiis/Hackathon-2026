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

var builder = WebApplication.CreateBuilder(args);

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
