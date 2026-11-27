using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using Hackathon_Docente_2026.Models;

namespace Hackathon_Docente_2026.Services;

/// <summary>Genera el ZIP de Canvas (QTI 1.2) con la misma estructura que un export real.</summary>
public class QtiExportService
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string Id() => Guid.NewGuid().ToString("N");
    static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "").Replace("&#39;", "&apos;");   // para XML
    static string Html(string? s) => Esc("<p>" + WebUtility.HtmlEncode(s ?? "") + "</p>");        // HTML escapado dentro de XML
    static string Texto(string? s) => Esc(WebUtility.HtmlEncode(s ?? ""));

    /// <summary>ZIP de prueba con datos fijos: una pregunta de cada uno de los 6 tipos.</summary>
    public byte[] GenerarZipDePrueba() => Exportar(DatosDePrueba());

    public byte[] Exportar(Examen examen)
    {
        var quizId = "g" + Id();
        var puntos = examen.Preguntas.Sum(p => p.Puntos);
        var items = new StringBuilder();
        foreach (var p in examen.Preguntas.OrderBy(p => p.Orden)) items.Append(Item(p));

        var qti = $"""
<?xml version="1.0"?>
<questestinterop xmlns="http://www.imsglobal.org/xsd/ims_qtiasiv1p2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.imsglobal.org/xsd/ims_qtiasiv1p2 http://www.imsglobal.org/xsd/ims_qtiasiv1p2p1.xsd">
  <assessment ident="{quizId}" title="{Esc(examen.Titulo)}">
    <qtimetadata>
      <qtimetadatafield>
        <fieldlabel>cc_maxattempts</fieldlabel>
        <fieldentry>1</fieldentry>
      </qtimetadatafield>
    </qtimetadata>
    <section ident="root_section">
{items}    </section>
  </assessment>
</questestinterop>
""";
        var meta = Meta(quizId, examen, puntos);
        var manifest = Manifest(quizId);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Add(zip, "imsmanifest.xml", manifest);
            Add(zip, $"{quizId}/assessment_meta.xml", meta);
            Add(zip, $"{quizId}/{quizId}.xml", qti);
        }
        return ms.ToArray();
    }

    static void Add(ZipArchive zip, string name, string content)
    {
        var e = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
        w.Write(content);
    }

    // ---------- manifest + meta ----------
    static string Manifest(string quizId)
    {
        var metaId = Id();
        return $"""
<?xml version="1.0"?>
<manifest xmlns="http://www.imsglobal.org/xsd/imsccv1p1/imscp_v1p1" xmlns:lom="http://ltsc.ieee.org/xsd/imsccv1p1/LOM/resource" xmlns:imsmd="http://www.imsglobal.org/xsd/imsmd_v1p2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" identifier="{Id()}" xsi:schemaLocation="http://www.imsglobal.org/xsd/imsccv1p1/imscp_v1p1 http://www.imsglobal.org/xsd/imscp_v1p1.xsd http://ltsc.ieee.org/xsd/imsccv1p1/LOM/resource http://www.imsglobal.org/profile/cc/ccv1p1/LOM/ccv1p1_lomresource_v1p0.xsd http://www.imsglobal.org/xsd/imsmd_v1p2 http://www.imsglobal.org/xsd/imsmd_v1p2p2.xsd">
  <metadata>
    <schema>IMS Content</schema>
    <schemaversion>1.1.3</schemaversion>
    <imsmd:lom>
      <imsmd:general>
        <imsmd:title>
          <imsmd:string>QTI Quiz Export for </imsmd:string>
        </imsmd:title>
      </imsmd:general>
      <imsmd:lifeCycle>
        <imsmd:contribute>
          <imsmd:date>
            <imsmd:dateTime>{DateTime.UtcNow:yyyy-MM-dd}</imsmd:dateTime>
          </imsmd:date>
        </imsmd:contribute>
      </imsmd:lifeCycle>
      <imsmd:rights>
        <imsmd:copyrightAndOtherRestrictions>
          <imsmd:value>yes</imsmd:value>
        </imsmd:copyrightAndOtherRestrictions>
        <imsmd:description>
          <imsmd:string>Private (Copyrighted) - http://en.wikipedia.org/wiki/Copyright</imsmd:string>
        </imsmd:description>
      </imsmd:rights>
    </imsmd:lom>
  </metadata>
  <organizations/>
  <resources>
    <resource identifier="{quizId}" type="imsqti_xmlv1p2">
      <file href="{quizId}/{quizId}.xml"/>
      <dependency identifierref="{metaId}"/>
    </resource>
    <resource identifier="{metaId}" type="associatedcontent/imscc_xmlv1p1/learning-application-resource" href="{quizId}/assessment_meta.xml">
      <file href="{quizId}/assessment_meta.xml"/>
    </resource>
  </resources>
</manifest>
""";
    }

    static string Meta(string quizId, Examen e, double puntos)
    {
        var pts = puntos.ToString("0.0##", Inv);
        var desc = string.IsNullOrWhiteSpace(e.Descripcion) ? "" : Html(e.Descripcion);
        return $"""
<?xml version="1.0"?>
<quiz xmlns="http://canvas.instructure.com/xsd/cccv1p0" xmlns:xsi="http://canvas.instructure.com/xsd/cccv1p0 https://canvas.instructure.com/xsd/cccv1p0.xsd" identifier="{quizId}">
  <title>{Esc(e.Titulo)}</title>
  <description>{desc}</description>
  <due_at/>
  <lock_at/>
  <unlock_at/>
  <shuffle_questions>false</shuffle_questions>
  <shuffle_answers>false</shuffle_answers>
  <calculator_type>none</calculator_type>
  <scoring_policy>keep_highest</scoring_policy>
  <hide_results/>
  <quiz_type>assignment</quiz_type>
  <points_possible>{pts}</points_possible>
  <require_lockdown_browser>false</require_lockdown_browser>
  <require_lockdown_browser_for_results>false</require_lockdown_browser_for_results>
  <require_lockdown_browser_monitor>false</require_lockdown_browser_monitor>
  <lockdown_browser_monitor_data/>
  <show_correct_answers>false</show_correct_answers>
  <anonymous_submissions>false</anonymous_submissions>
  <could_be_locked>false</could_be_locked>
  <disable_timer_autosubmission>false</disable_timer_autosubmission>
  <allowed_attempts>1</allowed_attempts>
  <build_on_last_attempt>false</build_on_last_attempt>
  <one_question_at_a_time>false</one_question_at_a_time>
  <cant_go_back>false</cant_go_back>
  <available>false</available>
  <one_time_results>false</one_time_results>
  <show_correct_answers_last_attempt>false</show_correct_answers_last_attempt>
  <only_visible_to_overrides>false</only_visible_to_overrides>
  <module_locked>false</module_locked>
  <allow_clear_mc_selection/>
  <disable_document_access>false</disable_document_access>
  <result_view_restricted>false</result_view_restricted>
  <assignment identifier="{Id()}">
    <title>{Esc(e.Titulo)}</title>
    <due_at/>
    <lock_at/>
    <unlock_at/>
    <module_locked>false</module_locked>
    <workflow_state>unpublished</workflow_state>
    <assignment_overrides/>
    <assignment_overrides/>
    <quiz_identifierref>{quizId}</quiz_identifierref>
    <allowed_extensions/>
    <has_group_category>false</has_group_category>
    <points_possible>{pts}</points_possible>
    <grading_type>points</grading_type>
    <all_day>false</all_day>
    <submission_types>online_quiz</submission_types>
    <position>1</position>
    <turnitin_enabled>false</turnitin_enabled>
    <vericite_enabled>false</vericite_enabled>
    <peer_review_count>0</peer_review_count>
    <peer_reviews>false</peer_reviews>
    <automatic_peer_reviews>false</automatic_peer_reviews>
    <anonymous_peer_reviews>false</anonymous_peer_reviews>
    <grade_group_students_individually>false</grade_group_students_individually>
    <freeze_on_copy>false</freeze_on_copy>
    <omit_from_final_grade>false</omit_from_final_grade>
    <intra_group_peer_reviews>false</intra_group_peer_reviews>
    <only_visible_to_overrides>false</only_visible_to_overrides>
    <post_to_sis>false</post_to_sis>
    <moderated_grading>false</moderated_grading>
    <grader_count>0</grader_count>
    <grader_comments_visible_to_graders>true</grader_comments_visible_to_graders>
    <anonymous_grading>false</anonymous_grading>
    <graders_anonymous_to_graders>false</graders_anonymous_to_graders>
    <grader_names_visible_to_final_grader>true</grader_names_visible_to_final_grader>
    <anonymous_instructor_annotations>false</anonymous_instructor_annotations>
    <post_policy>
      <post_manually>false</post_manually>
    </post_policy>
    <assignment_group_identifierref>g{Id()}</assignment_group_identifierref>
    <assignment_overrides/>
  </assignment>
</quiz>
""";
    }

    // ---------- items ----------
    static string Field(string label, string entry) =>
        entry.Length == 0
            ? $"            <qtimetadatafield>\n              <fieldlabel>{label}</fieldlabel>\n              <fieldentry/>\n            </qtimetadatafield>\n"
            : $"            <qtimetadatafield>\n              <fieldlabel>{label}</fieldlabel>\n              <fieldentry>{entry}</fieldentry>\n            </qtimetadatafield>\n";

    static string Feedback(string ident, string? html) =>
        $"        <itemfeedback ident=\"{ident}\">\n          <flow_mat>\n            <material>\n" +
        (string.IsNullOrWhiteSpace(html) ? "              <mattext texttype=\"text/html\"/>\n"
            : $"              <mattext texttype=\"text/html\">{Html(html)}</mattext>\n") +
        "            </material>\n          </flow_mat>\n        </itemfeedback>\n";

    static string Item(Pregunta p)
    {
        var sb = new StringBuilder();
        var tipo = p.Tipo.ToCanvas();
        var ops = p.Opciones.OrderBy(o => o.Orden).ToList();
        var rnd = new Random();

        // ids de respuesta y bloques específicos por tipo
        string answerIds = "", presentation, resproc, extraMeta = "";
        var enunciado = $"          <material>\n            <mattext texttype=\"text/html\">{Html(p.Enunciado)}</mattext>\n          </material>\n";
        const string outcomes = "          <outcomes>\n            <decvar maxvalue=\"100\" minvalue=\"0\" varname=\"SCORE\" vartype=\"Decimal\"/>\n          </outcomes>\n";

        switch (p.Tipo)
        {
            case TipoPregunta.VerdaderoFalso:
            {
                answerIds = "true_choice,false_choice";
                var correcta = ops.Count > 1 && ops[1].EsCorrecta ? "false_choice" : "true_choice";
                presentation = enunciado +
                    "          <response_lid ident=\"response1\" rcardinality=\"Single\">\n            <render_choice>\n" +
                    Label("true_choice", "Verdadero", 14, "text/html") + Label("false_choice", "Falso", 14, "text/html") +
                    "            </render_choice>\n          </response_lid>\n";
                resproc = outcomes + Cond(correcta);
                break;
            }
            case TipoPregunta.OpcionMultiple:
            {
                var ids = ops.Select(_ => Guid.NewGuid().ToString()).ToList();
                answerIds = string.Join(",", ids);
                var ci = Math.Max(0, ops.FindIndex(o => o.EsCorrecta));
                presentation = enunciado +
                    "          <response_lid ident=\"response1\" rcardinality=\"Single\">\n            <render_choice>\n" +
                    string.Concat(ops.Select((o, i) => Label(ids[i], o.Texto, 14, "text/html"))) +
                    "            </render_choice>\n          </response_lid>\n";
                resproc = outcomes + Cond(ids[ci]);
                break;
            }
            case TipoPregunta.Ensayo:
                extraMeta = Field("grading_notes", "");
                presentation = enunciado +
                    "          <response_str ident=\"response1\" rcardinality=\"Single\" rce=\"Yes\" word_count=\"No\" spell_check=\"No\" word_limit_enabled=\"No\">\n            <render_fib>\n              <response_label ident=\"answer1\" rshuffle=\"No\"/>\n            </render_fib>\n          </response_str>\n";
                resproc = outcomes + "          <respcondition continue=\"No\">\n            <conditionvar>\n              <other/>\n            </conditionvar>\n          </respcondition>\n";
                break;
            case TipoPregunta.Emparejamiento:
            {
                extraMeta = Field("scoring_algorithm", "PartialDeep");
                var pares = ops.Where(o => !string.IsNullOrWhiteSpace(o.Texto)).ToList();
                var distract = ops.Where(o => string.IsNullOrWhiteSpace(o.Texto) && !string.IsNullOrWhiteSpace(o.Pareja)).ToList();
                var usados = new HashSet<int>();
                var idPar = pares.Select(_ => { int n; do n = rnd.Next(10000, 99999); while (!usados.Add(n)); return n.ToString(); }).ToList();
                answerIds = string.Join(",", idPar);
                // lista de respuestas posibles (pares + distractores), barajada de forma determinista
                var choices = pares.Select((o, i) => (Id: idPar[i], Txt: o.Pareja ?? ""))
                    .Concat(distract.Select((o, i) => (Id: $"distractor_{i}", Txt: o.Pareja ?? "")))
                    .OrderBy(_ => rnd.Next()).ToList();
                var render = "            <render_choice>\n" +
                    string.Concat(choices.Select(c => Label(c.Id, c.Txt, 14, null))) + "            </render_choice>\n";
                var sbp = new StringBuilder(enunciado);
                for (var i = 0; i < pares.Count; i++)
                    sbp.Append($"          <response_lid ident=\"response_{idPar[i]}\">\n            <material>\n              <mattext texttype=\"text/plain\">{Texto(pares[i].Texto)}</mattext>\n            </material>\n{render}          </response_lid>\n");
                presentation = sbp.ToString();
                var score = (100.0 / Math.Max(1, pares.Count)).ToString("0.00", Inv);
                resproc = outcomes + string.Concat(idPar.Select(id =>
                    $"          <respcondition>\n            <conditionvar>\n              <varequal respident=\"response_{id}\">{id}</varequal>\n            </conditionvar>\n            <setvar action=\"Add\" varname=\"SCORE\">{score}</setvar>\n          </respcondition>\n"));
                break;
            }
            case TipoPregunta.Ordenamiento:
            {
                var ids = ops.Select(_ => Guid.NewGuid().ToString()).ToList();   // ids en orden correcto
                var vista = Enumerable.Range(0, ops.Count).Reverse().ToList();   // orden mostrado (invertido)
                if (ops.Count > 1 && vista.SequenceEqual(Enumerable.Range(0, ops.Count))) vista = vista.ToList();
                answerIds = string.Join(",", vista.Select(i => ids[i]));
                presentation = enunciado +
                    "          <response_lid ident=\"response1\" rcardinality=\"Ordered\">\n            <render_extension>\n" +
                    "              <material position=\"top\">\n                <mattext>Inicio</mattext>\n              </material>\n" +
                    "              <ims_render_object shuffle=\"No\">\n                <flow_label>\n" +
                    string.Concat(vista.Select(i => Label(ids[i], ops[i].Texto, 20, "text/html"))) +
                    "                </flow_label>\n              </ims_render_object>\n" +
                    "              <material position=\"bottom\">\n                <mattext>Fin</mattext>\n              </material>\n" +
                    "            </render_extension>\n          </response_lid>\n";
                resproc = "          <outcomes>\n            <decvar defaultval=\"1\" varname=\"ORDERSCORE\" vartype=\"Integer\"/>\n          </outcomes>\n" +
                    "          <respcondition continue=\"No\">\n            <conditionvar>\n" +
                    string.Concat(ids.Select(id => $"              <varequal respident=\"response1\">{id}</varequal>\n")) +
                    "            </conditionvar>\n            <setvar action=\"Set\" varname=\"SCORE\">100</setvar>\n          </respcondition>\n";
                break;
            }
            default: // SubidaArchivo
                presentation = enunciado;
                resproc = outcomes;
                break;
        }

        sb.Append($"      <item ident=\"{Id()}\" title=\"\">\n        <itemmetadata>\n          <qtimetadata>\n");
        sb.Append(Field("question_type", tipo));
        sb.Append(Field("points_possible", p.Puntos.ToString("0.0##", Inv)));
        sb.Append(Field("original_answer_ids", answerIds));
        sb.Append(Field("assessment_question_identifierref", Id()));
        sb.Append(Field("calculator_type", "none"));
        sb.Append(extraMeta);
        sb.Append("          </qtimetadata>\n        </itemmetadata>\n");
        sb.Append("        <presentation>\n").Append(presentation).Append("        </presentation>\n");
        sb.Append("        <resprocessing>\n").Append(resproc).Append("        </resprocessing>\n");
        sb.Append(Feedback("correct_fb", null)).Append(Feedback("general_fb", null))
          .Append(Feedback("general_incorrect_fb", p.Retroalimentacion));
        sb.Append("      </item>\n");
        return sb.ToString();
    }

    static string Label(string id, string texto, int indent, string? texttype)
    {
        var pad = new string(' ', indent);
        var tt = texttype == null ? "" : $" texttype=\"{texttype}\"";
        return $"{pad}<response_label ident=\"{id}\">\n{pad}  <material>\n{pad}    <mattext{tt}>{Texto(texto)}</mattext>\n{pad}  </material>\n{pad}</response_label>\n";
    }

    static string Cond(string correctId) =>
        $"          <respcondition continue=\"No\">\n            <conditionvar>\n              <varequal respident=\"response1\">{correctId}</varequal>\n            </conditionvar>\n            <setvar action=\"Set\" varname=\"SCORE\">100</setvar>\n          </respcondition>\n";

    // ---------- datos de prueba fijos ----------
    static Examen DatosDePrueba() => new()
    {
        Titulo = "Examen de prueba QTI",
        Descripcion = "Examen generado con datos fijos para validar la importación en Canvas.",
        Preguntas =
        {
            new Pregunta { Orden = 0, Tipo = TipoPregunta.VerdaderoFalso, Puntos = 1,
                Enunciado = "La fotosíntesis ocurre en los cloroplastos.",
                Retroalimentacion = "La fotosíntesis se realiza en los cloroplastos de las células vegetales.",
                Opciones = { new() { Texto = "Verdadero", EsCorrecta = true, Orden = 0 }, new() { Texto = "Falso", Orden = 1 } } },
            new Pregunta { Orden = 1, Tipo = TipoPregunta.OpcionMultiple, Puntos = 1,
                Enunciado = "¿Cuál es la capital de Colombia?",
                Retroalimentacion = "La capital de Colombia es Bogotá.",
                Opciones = { new() { Texto = "Bogotá", EsCorrecta = true, Orden = 0 }, new() { Texto = "Medellín", Orden = 1 },
                             new() { Texto = "Cali", Orden = 2 }, new() { Texto = "Barranquilla", Orden = 3 } } },
            new Pregunta { Orden = 2, Tipo = TipoPregunta.Ensayo, Puntos = 5,
                Enunciado = "Explica con tus palabras la importancia del ciclo del agua.",
                Retroalimentacion = "Revisa el material sobre el ciclo hidrológico." },
            new Pregunta { Orden = 3, Tipo = TipoPregunta.Emparejamiento, Puntos = 4,
                Enunciado = "Relaciona cada país con su capital.",
                Retroalimentacion = "Repasa las capitales de Sudamérica.",
                Opciones = { new() { Texto = "Perú", Pareja = "Lima", EsCorrecta = true, Orden = 0 },
                             new() { Texto = "Chile", Pareja = "Santiago", EsCorrecta = true, Orden = 1 },
                             new() { Texto = "Argentina", Pareja = "Buenos Aires", EsCorrecta = true, Orden = 2 },
                             new() { Texto = "Ecuador", Pareja = "Quito", EsCorrecta = true, Orden = 3 },
                             new() { Texto = "", Pareja = "Caracas", Orden = 4 },
                             new() { Texto = "", Pareja = "Montevideo", Orden = 5 } } },
            new Pregunta { Orden = 4, Tipo = TipoPregunta.Ordenamiento, Puntos = 4,
                Enunciado = "Ordena las etapas del método científico.",
                Retroalimentacion = "El orden es: observación, hipótesis, experimentación, conclusión.",
                Opciones = { new() { Texto = "Observación", EsCorrecta = true, Orden = 0 }, new() { Texto = "Hipótesis", EsCorrecta = true, Orden = 1 },
                             new() { Texto = "Experimentación", EsCorrecta = true, Orden = 2 }, new() { Texto = "Conclusión", EsCorrecta = true, Orden = 3 } } },
            new Pregunta { Orden = 5, Tipo = TipoPregunta.SubidaArchivo, Puntos = 5,
                Enunciado = "Sube un informe en PDF con los resultados de tu práctica.",
                Retroalimentacion = "Recuerda incluir introducción, método y conclusiones." },
        }
    };
}
