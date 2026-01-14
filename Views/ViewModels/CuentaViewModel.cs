using StreamManager.Data.Models;

namespace StreamManager.ViewModels
{
    public class CuentaViewModel
    {
        public Guid Id { get; set; }
        public Guid PlataformaId { get; set; }
        public string PlataformaNombre { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
        public int PerfilesDisponibles { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Notas { get; set; }

        // Propiedad completa
        public CuentaCorreo CuentaCorreo { get; set; } = new();
    }
}
