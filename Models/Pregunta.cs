namespace Hackathon_Docente_2026.Models;

public class Pregunta
{
    public int Id { get; set; }
    public int ExamenId { get; set; }
    public Examen? Examen { get; set; }
    public TipoPregunta Tipo { get; set; }
    public int Orden { get; set; }
    public string Enunciado { get; set; } = "";
    public double Puntos { get; set; } = 1;
    public string? Retroalimentacion { get; set; }
    public List<Opcion> Opciones { get; set; } = new();
}
