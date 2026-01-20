using StreamManager.Data.Models;

namespace StreamManager.ViewModels
{
    public class SuscripcionViewModel
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public Guid PerfilId { get; set; }
        public string PerfilNombre { get; set; } = string.Empty;
        public string PlataformaNombre { get; set; } = string.Empty;
        public decimal CostoMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime ProximoPago { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Notas { get; set; }
        public Suscripcion Suscripcion { get; set; } = new();
    }
}