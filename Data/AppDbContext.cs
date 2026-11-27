using Hackathon_Docente_2026.Models;
using Microsoft.EntityFrameworkCore;

namespace Hackathon_Docente_2026.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Examen> Examenes => Set<Examen>();
    public DbSet<Pregunta> Preguntas => Set<Pregunta>();
    public DbSet<Opcion> Opciones => Set<Opcion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Examen>().HasMany(e => e.Preguntas).WithOne(p => p.Examen!)
            .HasForeignKey(p => p.ExamenId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Pregunta>().HasMany(p => p.Opciones).WithOne(o => o.Pregunta!)
            .HasForeignKey(o => o.PreguntaId).OnDelete(DeleteBehavior.Cascade);
    }
}
