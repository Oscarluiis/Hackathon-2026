# Hackathon Docente 2026

Herramienta web para que un docente suba material en PDF y genere automáticamente un examen en formato **Canvas QTI 1.2**, listo para importar como ZIP.

**Stack:** .NET 10 · Blazor Server · SQLite + EF Core · PdfPig · Google Gemini.

## Requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Una API key de Google Gemini (https://aistudio.google.com/apikey)

## Configuración
La API key **no se guarda en el repositorio**: se lee de la variable de entorno `GEMINI_API_KEY`.

```bash
export GEMINI_API_KEY="tu_clave"      # Linux / macOS
$env:GEMINI_API_KEY="tu_clave"        # PowerShell
```

Otros ajustes en `appsettings.json`:

| Clave | Descripción |
|---|---|
| `Auth:Usuario` / `Auth:Password` | Credenciales del login. **Cámbialas** (por defecto `docente` / `cambiar123`). |
| `Gemini:Model` | Modelo de Gemini a usar. |
| `Gemini:FallbackModel` | Modelo alterno si el principal está saturado (opcional). |
| `ConnectionStrings:Default` | Ruta de la base SQLite (`hackathon.db`, se crea sola). |

## Ejecutar
```bash
dotnet run
```
La base de datos se crea y migra automáticamente al arrancar.

## Flujo
1. Login → 2. Lista de exámenes → 3. Nuevo examen (PDF, tipos, cantidades y puntos) → 4. Revisión y exportación a Canvas (`.zip`).

Tipos soportados: verdadero/falso, opción múltiple, ensayo, emparejamiento, ordenamiento y subida de archivo.

## ZIP de prueba (sin IA)
```bash
dotnet run -- --qti-test prueba.zip
```
Genera un ZIP con una pregunta de cada tipo para validar la importación en Canvas.

## Estructura
```
Components/   Páginas y componentes Blazor
Data/         DbContext de EF Core
Migrations/   Migraciones de EF Core
Models/       Examen, Pregunta, Opción, TipoPregunta
Services/     PdfExtractionService, QuestionGenerationService, QtiExportService
wwwroot/      CSS, scripts y logo
```
