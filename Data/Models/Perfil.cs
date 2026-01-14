using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("perfiles")]
    public class Perfil : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("cuenta_id")]
        public Guid CuentaId { get; set; }

        [Column("nombre_perfil")]
        public string NombrePerfil { get; set; } = string.Empty;

        [Column("pin")]
        public string? Pin { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "disponible";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_asignacion")]
        public DateTime? FechaAsignacion { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }
    }
}