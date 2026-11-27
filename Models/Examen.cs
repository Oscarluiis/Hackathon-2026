namespace Hackathon_Docente_2026.Models;

public class Examen
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? TextoFuente { get; set; }
    public string? Instrucciones { get; set; }
    public string Usuario { get; set; } = "";
    public DateTime Creado { get; set; } = DateTime.UtcNow;
    public List<Pregunta> Preguntas { get; set; } = new();
}
