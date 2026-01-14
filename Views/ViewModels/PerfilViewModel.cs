using StreamManager.Data.Models;

namespace StreamManager.ViewModels
{
    public class PerfilViewModel
    {
        public Guid Id { get; set; }
        public Guid CuentaCorreoId { get; set; }
        public string CuentaNombre { get; set; } = string.Empty;
        public string PlataformaNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // Propiedad completa
        public Perfil Perfil { get; set; } = new();
    }
}
