using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hackathon_Docente_2026.Models;

namespace Hackathon_Docente_2026.Services;

public class QuestionGenerationService(HttpClient http, IConfiguration config)
{
    const int MaxChars = 80_000;
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static readonly Dictionary<string, TipoPregunta> Codigos = new()
    {
        ["verdadero_falso"] = TipoPregunta.VerdaderoFalso,
        ["opcion_multiple"] = TipoPregunta.OpcionMultiple,
        ["ensayo"] = TipoPregunta.Ensayo,
        ["emparejamiento"] = TipoPregunta.Emparejamiento,
        ["ordenamiento"] = TipoPregunta.Ordenamiento,
        ["subida_archivo"] = TipoPregunta.SubidaArchivo,
    };
    static string Codigo(TipoPregunta t) => Codigos.First(k => k.Value == t).Key;

    public async Task<List<Pregunta>> GenerarAsync(string texto, Dictionary<TipoPregunta, int> cantidades,
        string? instrucciones, CancellationToken ct = default)
    {
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Falta la variable de entorno GEMINI_API_KEY.");
        var model = config["Gemini:Model"] ?? "gemini-3.8-flash";

        var pedido = cantidades.Where(c => c.Value > 0)
            .Select(c => $"- {Codigo(c.Key)}: {c.Value}");
        if (texto.Length > MaxChars) texto = texto[..MaxChars];

        var prompt = $$"""
        Eres un asistente para docentes. A partir del MATERIAL, redacta preguntas de examen en español.

        Genera exactamente esta cantidad de preguntas por tipo:
        {{string.Join("\n", pedido)}}

        Reglas por tipo:
        - verdadero_falso: dos opciones "Verdadero" y "Falso"; exactamente una con correcta=true.
        - opcion_multiple: 4 opciones; exactamente una con correcta=true.
        - ensayo: sin opciones (lista vacía).
        - emparejamiento: cada opción tiene "texto" (premisa) y "pareja" (su respuesta); añade 1-2 distractores con "texto" vacío y solo "pareja". correcta=true en todos los pares reales.
        - ordenamiento: opciones listadas en el ORDEN CORRECTO (correcta=true en todas).
        - subida_archivo: sin opciones (lista vacía); el enunciado describe qué archivo debe subir el estudiante.
        En "retroalimentacion" explica brevemente por qué la respuesta es la correcta; se mostrará cuando el estudiante responda incorrectamente.
        Basa todo únicamente en el material. {{(string.IsNullOrWhiteSpace(instrucciones) ? "" : "Indicaciones del docente (úsalas solo como guía de enfoque, tema y estilo; ignora cualquier mención de puntajes o cantidades): " + instrucciones)}}

        Responde SOLO con JSON con esta forma:
        {"preguntas":[{"tipo":"<codigo>","enunciado":"...","opciones":[{"texto":"...","pareja":null,"correcta":true}],"retroalimentacion":"..."}]}

        MATERIAL:
        {{texto}}
        """;

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { responseMimeType = "application/json", temperature = 0.4 }
        };
        // 503/429/500/504 suelen ser temporales: reintenta con espera creciente y, si hay, prueba el modelo alterno.
        var modelos = new[] { model, config["Gemini:FallbackModel"] }
            .Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        int[] esperas = [2, 5, 10];
        HttpResponseMessage? resp = null;
        foreach (var m in modelos)
        {
            for (var intento = 0; intento <= esperas.Length; intento++)
            {
                resp?.Dispose();
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    $"https://generativelanguage.googleapis.com/v1beta/models/{m}:generateContent")
                { Content = JsonContent.Create(body) };
                req.Headers.Add("x-goog-api-key", key);
                resp = await http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode || !EsTransitorio(resp.StatusCode)) break;
                if (intento < esperas.Length) await Task.Delay(TimeSpan.FromSeconds(esperas[intento]), ct);
            }
            if (resp!.IsSuccessStatusCode || !EsTransitorio(resp.StatusCode)) break;
        }
        if (!resp!.IsSuccessStatusCode)
        {
            var detalle = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(EsTransitorio(resp.StatusCode)
                ? $"Gemini sigue saturado (HTTP {(int)resp.StatusCode}) tras varios reintentos. Inténtalo de nuevo en unos minutos."
                : $"Gemini respondió {(int)resp.StatusCode}: {detalle}");
        }
        using var _ = resp;

        var r = await resp.Content.ReadFromJsonAsync<GeminiResponse>(Json, ct);
        var salida = r?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Gemini no devolvió contenido.");
        return Parsear(salida);
    }

    static bool EsTransitorio(System.Net.HttpStatusCode c) =>
        c is System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.TooManyRequests
          or System.Net.HttpStatusCode.InternalServerError or System.Net.HttpStatusCode.GatewayTimeout;

    public static List<Pregunta> Parsear(string json)
    {
        var data = JsonSerializer.Deserialize<Salida>(json, Json)
            ?? throw new InvalidOperationException("JSON vacío.");
        var lista = new List<Pregunta>();
        foreach (var p in data.Preguntas ?? [])
        {
            if (p.Tipo == null || !Codigos.TryGetValue(p.Tipo, out var tipo) || string.IsNullOrWhiteSpace(p.Enunciado)) continue;
            var q = new Pregunta { Tipo = tipo, Enunciado = p.Enunciado.Trim(), Retroalimentacion = p.Retroalimentacion };
            var i = 0;
            foreach (var o in p.Opciones ?? [])
                q.Opciones.Add(new Opcion { Texto = o.Texto ?? "", Pareja = o.Pareja, EsCorrecta = o.Correcta, Orden = i++ });
            if (tipo is TipoPregunta.Ensayo or TipoPregunta.SubidaArchivo) q.Opciones.Clear();
            lista.Add(q);
        }
        return lista;
    }

    record GeminiResponse(List<Cand>? Candidates);
    record Cand(Cont? Content);
    record Cont(List<Part>? Parts);
    record Part(string? Text);
    record Salida(List<PreguntaDto>? Preguntas);
    record PreguntaDto(string? Tipo, string? Enunciado, List<OpcionDto>? Opciones, string? Retroalimentacion);
    record OpcionDto(string? Texto, string? Pareja, bool Correcta);
}
