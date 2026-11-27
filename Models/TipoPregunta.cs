namespace Hackathon_Docente_2026.Models;

public enum TipoPregunta
{
    VerdaderoFalso,
    OpcionMultiple,
    Ensayo,
    Emparejamiento,
    Ordenamiento,
    SubidaArchivo
}

public static class TipoPreguntaExtensions
{
    /// <summary>Valor de question_type en Canvas QTI.</summary>
    public static string ToCanvas(this TipoPregunta t) => t switch
    {
        TipoPregunta.VerdaderoFalso => "true_false_question",
        TipoPregunta.OpcionMultiple => "multiple_choice_question",
        TipoPregunta.Ensayo => "essay_question",
        TipoPregunta.Emparejamiento => "matching_question",
        TipoPregunta.Ordenamiento => "ordering_question",
        TipoPregunta.SubidaArchivo => "file_upload_question",
        _ => throw new ArgumentOutOfRangeException(nameof(t))
    };

    public static string Etiqueta(this TipoPregunta t) => t switch
    {
        TipoPregunta.VerdaderoFalso => "Verdadero/Falso",
        TipoPregunta.OpcionMultiple => "Opción Múltiple",
        TipoPregunta.Ensayo => "Ensayo",
        TipoPregunta.Emparejamiento => "Emparejamiento",
        TipoPregunta.Ordenamiento => "Ordenamiento",
        TipoPregunta.SubidaArchivo => "Subida de Archivo",
        _ => t.ToString()
    };
}
