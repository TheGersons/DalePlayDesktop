
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StreamManager.Data.Models
{
    [Table("cuentas_correo")]
    public class CuentaCorreo : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("plataforma_id")]
        public Guid PlataformaId { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "activo";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }
    }
}