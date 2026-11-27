namespace Hackathon_Docente_2026.Models;

/// <summary>
/// Opción de respuesta. En emparejamiento, Texto es la premisa y Pareja su respuesta
/// (una Opcion sin Texto y solo con Pareja es un distractor). En ordenamiento, Orden indica la posición correcta.
/// </summary>
public class Opcion
{
    public int Id { get; set; }
    public int PreguntaId { get; set; }
    public Pregunta? Pregunta { get; set; }
    public string Texto { get; set; } = "";
    public bool EsCorrecta { get; set; }
    public string? Pareja { get; set; }
    public int Orden { get; set; }
}
