namespace Hackathon_Docente_2026.Services;

/// <summary>Sesión simple por circuito de Blazor (se pierde al recargar la página).</summary>
public class AuthState(IConfiguration config)
{
    public string? Usuario { get; private set; }
    public bool EstaAutenticado => Usuario != null;

    public bool Login(string usuario, string password)
    {
        var u = config["Auth:Usuario"]; var p = config["Auth:Password"];
        if (usuario == u && password == p) { Usuario = usuario; return true; }
        return false;
    }
    public void Logout() => Usuario = null;
}
